﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using EMBC.ESS.Utilities.Dynamics;
using EMBC.ESS.Utilities.Dynamics.Microsoft.Dynamics.CRM;
using EMBC.Utilities;
using Microsoft.OData.Client;

namespace EMBC.ESS.Resources.Evacuations
{
    public class EvacuationRepository : IEvacuationRepository
    {
        private readonly IEssContextFactory essContextFactory;
        private readonly IMapper mapper;

        public EvacuationRepository(IEssContextFactory essContextFactory, IMapper mapper)
        {
            this.essContextFactory = essContextFactory;
            this.mapper = mapper;
        }

        public async Task<ManageEvacuationFileCommandResult> Manage(ManageEvacuationFileCommand cmd)
        {
            return cmd switch
            {
                SubmitEvacuationFileNeedsAssessment c => await HandleSubmitEvacuationFileNeedsAssessment(c),
                LinkEvacuationFileRegistrant c => await HandleLinkEvacuationFileRegistrant(c),
                SaveEvacuationFileNote c => await HandleSaveEvacuationFileNote(c),
                _ => throw new NotSupportedException($"{cmd.GetType().Name} is not supported")
            };
        }

        public async Task<EvacuationFileQueryResult> Query(EvacuationFilesQuery query)
        {
            return query switch
            {
                EvacuationFilesQuery q => await HandleQueryEvacuationFile(q),
                _ => throw new NotSupportedException($"{query.GetType().Name} is not supported")
            };
        }

        private async Task<EvacuationFileQueryResult> HandleQueryEvacuationFile(EvacuationFilesQuery query)
        {
            var result = new EvacuationFileQueryResult();

            result.Items = await Read(query);

            return result;
        }

        private async Task<ManageEvacuationFileCommandResult> HandleSubmitEvacuationFileNeedsAssessment(SubmitEvacuationFileNeedsAssessment cmd)
        {
            var ctx = essContextFactory.Create();
            var ct = new CancellationTokenSource().Token;

            if (string.IsNullOrEmpty(cmd.EvacuationFile.Id))
            {
                return new ManageEvacuationFileCommandResult { Id = await Create(ctx, cmd.EvacuationFile, ct) };
            }
            else
            {
                return new ManageEvacuationFileCommandResult { Id = await Update(ctx, cmd.EvacuationFile, ct) };
            }
        }

        private async Task<ManageEvacuationFileCommandResult> HandleLinkEvacuationFileRegistrant(LinkEvacuationFileRegistrant cmd)
        {
            var ctx = essContextFactory.Create();
            var ct = new CancellationTokenSource().Token;

            return new ManageEvacuationFileCommandResult { Id = await LinkRegistrant(ctx, cmd.FileId, cmd.RegistrantId, cmd.HouseholdMemberId, ct) };
        }

        private async Task<ManageEvacuationFileCommandResult> HandleSaveEvacuationFileNote(SaveEvacuationFileNote cmd)
        {
            var ctx = essContextFactory.Create();
            var ct = new CancellationTokenSource().Token;

            if (string.IsNullOrEmpty(cmd.Note.Id))
            {
                return new ManageEvacuationFileCommandResult { Id = await CreateNote(ctx, cmd.FileId, cmd.Note, ct) };
            }
            else
            {
                return new ManageEvacuationFileCommandResult { Id = await UpdateNote(ctx, cmd.FileId, cmd.Note, ct) };
            }
        }

        public async Task<string> Create(EssContext essContext, EvacuationFile evacuationFile, CancellationToken ct)
        {
            VerifyEvacuationFileInvariants(evacuationFile);

            var primaryContact = essContext.contacts.Where(c => c.statecode == (int)EntityState.Active && c.contactid == Guid.Parse(evacuationFile.PrimaryRegistrantId)).SingleOrDefault();
            if (primaryContact == null) throw new ArgumentException($"Primary registrant {evacuationFile.PrimaryRegistrantId} not found");

            var file = mapper.Map<era_evacuationfile>(evacuationFile);
            file.era_evacuationfileid = Guid.NewGuid();

            essContext.AddToera_evacuationfiles(file);
            essContext.SetLink(file, nameof(era_evacuationfile.era_EvacuatedFromID), essContext.LookupJurisdictionByCode(file._era_evacuatedfromid_value?.ToString()));
            AssignPrimaryRegistrant(essContext, file, primaryContact);
            AssignToTask(essContext, file, evacuationFile.TaskId);
            AddPets(essContext, file);

            AddNeedsAssessment(essContext, file, file.era_CurrentNeedsAssessmentid);

            await essContext.SaveChangesAsync(ct);

            essContext.Detach(file);

            //get the autogenerated evacuation file number
            var essFileNumber = essContext.era_evacuationfiles.Where(f => f.era_evacuationfileid == file.era_evacuationfileid).Select(f => f.era_name).Single();

            essContext.DetachAll();

            return essFileNumber;
        }

        public async Task<string> Update(EssContext essContext, EvacuationFile evacuationFile, CancellationToken ct)
        {
            var currentFile = essContext.era_evacuationfiles
                .Where(f => f.era_name == evacuationFile.Id).SingleOrDefault();
            if (currentFile == null) throw new ArgumentException($"Evacuation file {evacuationFile.Id} not found");

            await essContext.LoadPropertyAsync(currentFile, nameof(era_evacuationfile.era_era_evacuationfile_era_householdmember_EvacuationFileid), ct);
            await essContext.LoadPropertyAsync(currentFile, nameof(era_evacuationfile.era_era_evacuationfile_era_animal_ESSFileid), ct);
            VerifyEvacuationFileInvariants(evacuationFile, currentFile);

            essContext.DetachAll();
            RemovePets(essContext, currentFile);

            var file = mapper.Map<era_evacuationfile>(evacuationFile);
            file.era_evacuationfileid = currentFile.era_evacuationfileid;

            essContext.AttachTo(nameof(essContext.era_evacuationfiles), file);
            essContext.SetLink(file, nameof(era_evacuationfile.era_EvacuatedFromID), essContext.LookupJurisdictionByCode(file._era_evacuatedfromid_value?.ToString()));

            essContext.UpdateObject(file);
            AssignToTask(essContext, file, evacuationFile.TaskId);
            AddPets(essContext, file);

            AddNeedsAssessment(essContext, file, file.era_CurrentNeedsAssessmentid);

            await essContext.SaveChangesAsync(ct);

            essContext.DetachAll();

            return file.era_name;
        }

        private static void AddNeedsAssessment(EssContext essContext, era_evacuationfile file, era_needassessment needsAssessment)
        {
            essContext.AddToera_needassessments(needsAssessment);
            essContext.SetLink(file, nameof(era_evacuationfile.era_CurrentNeedsAssessmentid), needsAssessment);
            essContext.AddLink(file, nameof(era_evacuationfile.era_needsassessment_EvacuationFile), needsAssessment);
            essContext.SetLink(needsAssessment, nameof(era_needassessment.era_EvacuationFile), file);
            essContext.SetLink(needsAssessment, nameof(era_needassessment.era_Jurisdictionid), essContext.LookupJurisdictionByCode(needsAssessment._era_jurisdictionid_value?.ToString()));
            if (needsAssessment._era_reviewedbyid_value.HasValue)
            {
                var teamMember = essContext.era_essteamusers.ByKey(needsAssessment._era_reviewedbyid_value).GetValue();
                essContext.SetLink(needsAssessment, nameof(era_needassessment.era_ReviewedByid), teamMember);
                essContext.AddLink(teamMember, nameof(era_essteamuser.era_era_essteamuser_era_needassessment_ReviewedByid), needsAssessment);
            }

            foreach (var member in needsAssessment.era_era_householdmember_era_needassessment)
            {
                if (member.era_householdmemberid.HasValue)
                {
                    //update member
                    essContext.AttachTo(nameof(essContext.era_householdmembers), member);
                    essContext.UpdateObject(member);
                }
                else
                {
                    //create member
                    member.era_householdmemberid = Guid.NewGuid();
                    essContext.AddToera_householdmembers(member);
                }
                AssignHouseholdMember(essContext, file, member);
                AssignHouseholdMember(essContext, needsAssessment, member);
            }
        }

        private static void AssignPrimaryRegistrant(EssContext essContext, era_evacuationfile file, contact primaryContact)
        {
            essContext.AddLink(primaryContact, nameof(primaryContact.era_evacuationfile_Registrant), file);
            essContext.SetLink(file, nameof(era_evacuationfile.era_Registrant), primaryContact);
        }

        private static void AssignToTask(EssContext essContext, era_evacuationfile file, string taskId)
        {
            if (string.IsNullOrEmpty(taskId)) return;
            var task = essContext.era_tasks.Where(t => t.era_name == taskId).SingleOrDefault();
            if (task == null) throw new ArgumentException($"Task {taskId} not found");
            essContext.AddLink(task, nameof(era_task.era_era_task_era_evacuationfileId), file);
        }

        private static void AssignHouseholdMember(EssContext essContext, era_evacuationfile file, era_householdmember member)
        {
            if (member._era_registrant_value != null)
            {
                var registrant = essContext.contacts.Where(c => c.contactid == member._era_registrant_value).SingleOrDefault();
                if (registrant == null) throw new ArgumentException($"Household member has registrant id {member._era_registrant_value} which was not found");
                essContext.AddLink(registrant, nameof(contact.era_contact_era_householdmember_Registrantid), member);
            }
            essContext.AddLink(file, nameof(era_evacuationfile.era_era_evacuationfile_era_householdmember_EvacuationFileid), member);
            essContext.SetLink(member, nameof(era_householdmember.era_EvacuationFileid), file);
        }

        private static void AssignHouseholdMember(EssContext essContext, era_needassessment needsAssessment, era_householdmember member)
        {
            essContext.AddLink(member, nameof(era_householdmember.era_era_householdmember_era_needassessment), needsAssessment);
        }

        private static void AddPets(EssContext essContext, era_evacuationfile file)
        {
            foreach (var pet in file.era_era_evacuationfile_era_animal_ESSFileid)
            {
                essContext.AddToera_animals(pet);
                essContext.AddLink(file, nameof(era_evacuationfile.era_era_evacuationfile_era_animal_ESSFileid), pet);
                essContext.SetLink(pet, nameof(era_animal.era_ESSFileid), file);
            }
        }

        private static void RemovePets(EssContext essContext, era_evacuationfile file)
        {
            foreach (var pet in file.era_era_evacuationfile_era_animal_ESSFileid)
            {
                essContext.AttachTo(nameof(essContext.era_animals), pet);
                essContext.DeleteObject(pet);
            }
        }

        private static void VerifyEvacuationFileInvariants(EvacuationFile evacuationFile, era_evacuationfile currentFile = null)
        {
            //Check invariants
            if (string.IsNullOrEmpty(evacuationFile.PrimaryRegistrantId))
            {
                throw new ArgumentNullException($"The file has no associated primary registrant");
            }
            if (evacuationFile.NeedsAssessment == null)
            {
                throw new ArgumentNullException($"File {evacuationFile.Id} must have a needs assessment");
            }

            if (evacuationFile.Id == null)
            {
                if (evacuationFile.NeedsAssessment.HouseholdMembers.Count(m => m.IsPrimaryRegistrant) != 1)
                {
                    throw new InvalidOperationException($"File {evacuationFile.Id} must have a single primary registrant household member");
                }
            }
            else
            {
                if (evacuationFile.NeedsAssessment.HouseholdMembers.Count(m => m.IsPrimaryRegistrant) > 1)
                {
                    throw new InvalidOperationException($"File {evacuationFile.Id} can not have multiple primary registrant household members");
                }

                if (currentFile != null && currentFile.era_era_evacuationfile_era_householdmember_EvacuationFileid != null &&
                    currentFile.era_era_evacuationfile_era_householdmember_EvacuationFileid.Any(m => m.era_isprimaryregistrant == true) &&
                    evacuationFile.NeedsAssessment.HouseholdMembers.Any(m => m.IsPrimaryRegistrant && string.IsNullOrEmpty(m.Id)))
                {
                    throw new InvalidOperationException($"File {evacuationFile.Id} can not have multiple primary registrant household members");
                }
            }
        }

        private static async Task<string> LinkRegistrant(EssContext essContext, string fileId, string registrantId, string householdMemberId, CancellationToken ct)
        {
            var member = essContext.era_householdmembers.Where(m => m.era_householdmemberid == Guid.Parse(householdMemberId)).SingleOrDefault();
            if (member == null) throw new ArgumentException($"Household member with id {householdMemberId} not found");
            var registrant = essContext.contacts.Where(c => c.contactid == Guid.Parse(registrantId)).SingleOrDefault();
            if (registrant == null) throw new ArgumentException($"Registrant with id {registrantId} not found");

            essContext.AddLink(registrant, nameof(contact.era_contact_era_householdmember_Registrantid), member);
            await essContext.SaveChangesAsync(ct);

            return fileId;
        }

        private static async Task ParallelLoadEvacuationFileAsync(EssContext ctx, era_evacuationfile file, CancellationToken ct)
        {
            ctx.AttachTo(nameof(EssContext.era_evacuationfiles), file);

            var loadTasks = new List<Task>();
            loadTasks.Add(ctx.LoadPropertyAsync(file, nameof(era_evacuationfile.era_era_evacuationfile_era_animal_ESSFileid), ct));
            loadTasks.Add(ctx.LoadPropertyAsync(file, nameof(era_evacuationfile.era_era_evacuationfile_era_essfilenote_ESSFileID), ct));
            loadTasks.Add(ctx.LoadPropertyAsync(file, nameof(era_evacuationfile.era_TaskId), ct));

            loadTasks.Add(ctx.LoadPropertyAsync(file, nameof(era_evacuationfile.era_era_evacuationfile_era_evacueesupport_ESSFileId), ct));

            loadTasks.Add(Task.Run(async () =>
            {
                if (file.era_CurrentNeedsAssessmentid == null) await ctx.LoadPropertyAsync(file, nameof(era_evacuationfile.era_CurrentNeedsAssessmentid), ct);

                var members = await ((DataServiceQuery<era_householdmember>)ctx.era_householdmembers
                    .Expand(m => m.era_Registrant)
                    .Where(m => m._era_evacuationfileid_value == file.era_evacuationfileid))
                    .GetAllPagesAsync(ct);

                file.era_era_evacuationfile_era_householdmember_EvacuationFileid = new Collection<era_householdmember>(
                    members.Where(m => m.era_Registrant == null || m.era_Registrant.statecode == (int)EntityState.Active).ToArray());

                var naHouseholdMembers = (await ((DataServiceQuery<era_era_householdmember_era_needassessment>)ctx.era_era_householdmember_era_needassessmentset
                    .Where(m => m.era_needassessmentid == file._era_currentneedsassessmentid_value))
                    .GetAllPagesAsync(ct))
                    .ToArray();

                file.era_CurrentNeedsAssessmentid.era_era_householdmember_era_needassessment = new Collection<era_householdmember>(
                    file.era_era_evacuationfile_era_householdmember_EvacuationFileid
                        .Where(m => naHouseholdMembers.Any(nam => nam.era_householdmemberid == m.era_householdmemberid)).ToArray());
            }));
            await Task.WhenAll(loadTasks);
        }

        private async Task<IEnumerable<EvacuationFile>> Read(EvacuationFilesQuery query)
        {
            var ct = new CancellationTokenSource().Token;
            var readCtx = essContextFactory.CreateReadOnly();

            //get all matching files
            var files = (await QueryHouseholdMemberFiles(readCtx, query, ct))
                .Concat(await QueryEvacuationFiles(readCtx, query, ct))
                .Concat(await QueryNeedsAssessments(readCtx, query, ct));

            //secondary filter after loading the files
            if (!string.IsNullOrEmpty(query.FileId)) files = files.Where(f => f.era_name == query.FileId);
            if (query.RegistraionDateFrom.HasValue) files = files.Where(f => f.createdon.Value.UtcDateTime >= query.RegistraionDateFrom.Value);
            if (query.RegistraionDateTo.HasValue) files = files.Where(f => f.createdon.Value.UtcDateTime <= query.RegistraionDateTo.Value);
            if (query.IncludeFilesInStatuses.Any()) files = files.Where(f => query.IncludeFilesInStatuses.Any(s => (int)s == f.era_essfilestatus));
            if (query.Limit.HasValue) files = files.OrderByDescending(f => f.era_name).Take(query.Limit.Value);

            //ensure files will be loaded only once
            files = files
                .Distinct(new LambdaComparer<era_evacuationfile>((f1, f2) => f1.era_evacuationfileid == f2.era_evacuationfileid, f => f.era_evacuationfileid.GetHashCode()))
                .ToArray();

            //return (await ParallelLoadEvacuationFilesAsync(readCtx, files)).Select(f => MapEvacuationFile(f, query.MaskSecurityPhrase)).ToArray();
            await Parallel.ForEachAsync(files, ct, async (f, ct) => await ParallelLoadEvacuationFileAsync(readCtx, f, ct));

            return mapper.Map<IEnumerable<EvacuationFile>>(files, opt => opt.Items["MaskSecurityPhrase"] = query.MaskSecurityPhrase.ToString());
        }

        private static async Task<IEnumerable<era_evacuationfile>> QueryHouseholdMemberFiles(EssContext ctx, EvacuationFilesQuery query, CancellationToken ct)
        {
            var shouldQueryHouseholdMembers =
                string.IsNullOrEmpty(query.FileId) && string.IsNullOrEmpty(query.NeedsAssessmentId) &&
             (!string.IsNullOrEmpty(query.LinkedRegistrantId) ||
             !string.IsNullOrEmpty(query.PrimaryRegistrantId) ||
             !string.IsNullOrEmpty(query.HouseholdMemberId));

            if (!shouldQueryHouseholdMembers) return Array.Empty<era_evacuationfile>();

            var memberQuery = ctx.era_householdmembers
                .Expand(m => m.era_EvacuationFileid)
                .Where(m => m.statecode == (int)EntityState.Active);

            if (!string.IsNullOrEmpty(query.PrimaryRegistrantId)) memberQuery = memberQuery.Where(m => m.era_isprimaryregistrant == true && m._era_registrant_value == Guid.Parse(query.PrimaryRegistrantId));
            if (!string.IsNullOrEmpty(query.HouseholdMemberId)) memberQuery = memberQuery.Where(m => m.era_householdmemberid == Guid.Parse(query.HouseholdMemberId));
            if (!string.IsNullOrEmpty(query.LinkedRegistrantId)) memberQuery = memberQuery.Where(m => m._era_registrant_value == Guid.Parse(query.LinkedRegistrantId));

            return (await ((DataServiceQuery<era_householdmember>)memberQuery).GetAllPagesAsync(ct))
                .Select(m => m.era_EvacuationFileid)
                .Where(f => f.statecode == (int)EntityState.Active);
        }

        private static async Task<IEnumerable<era_evacuationfile>> QueryEvacuationFiles(EssContext ctx, EvacuationFilesQuery query, CancellationToken ct)
        {
            var shouldQueryFiles =
                string.IsNullOrEmpty(query.NeedsAssessmentId) &&
                (!string.IsNullOrEmpty(query.FileId) ||
                !string.IsNullOrEmpty(query.ManualFileId) ||
                query.RegistraionDateFrom.HasValue ||
                query.RegistraionDateTo.HasValue);

            if (!shouldQueryFiles) return Array.Empty<era_evacuationfile>();

            var filesQuery = ctx.era_evacuationfiles.Expand(f => f.era_CurrentNeedsAssessmentid).Where(f => f.statecode == (int)EntityState.Active);

            if (!string.IsNullOrEmpty(query.FileId)) filesQuery = filesQuery.Where(f => f.era_name == query.FileId);
            if (!string.IsNullOrEmpty(query.ManualFileId)) filesQuery = filesQuery.Where(f => f.era_paperbasedessfile == query.ManualFileId);
            if (query.RegistraionDateFrom.HasValue) filesQuery = filesQuery.Where(f => f.createdon >= query.RegistraionDateFrom.Value);
            if (query.RegistraionDateTo.HasValue) filesQuery = filesQuery.Where(f => f.createdon <= query.RegistraionDateTo.Value);

            return await ((DataServiceQuery<era_evacuationfile>)filesQuery).GetAllPagesAsync(ct);
        }

        private static async Task<IEnumerable<era_evacuationfile>> QueryNeedsAssessments(EssContext ctx, EvacuationFilesQuery query, CancellationToken ct)
        {
            var shouldQueryNeedsAssessments = !string.IsNullOrEmpty(query.NeedsAssessmentId) && !string.IsNullOrEmpty(query.FileId);

            if (!shouldQueryNeedsAssessments) return Array.Empty<era_evacuationfile>();

            var needsAssessmentQuery = ctx.era_needassessments.Expand(n => n.era_EvacuationFile).Where(n => n.statecode == (int)EntityState.Active);

            if (!string.IsNullOrEmpty(query.NeedsAssessmentId)) needsAssessmentQuery = needsAssessmentQuery.Where(n => n.era_needassessmentid == Guid.Parse(query.NeedsAssessmentId));

            return (await ((DataServiceQuery<era_needassessment>)needsAssessmentQuery)
                .GetAllPagesAsync(ct))
                .ToArray()
                .Where(n => n.era_EvacuationFile.era_name == query.FileId && n.era_EvacuationFile.statecode == (int)EntityState.Active)
                .Select(n =>
                {
                    n.era_EvacuationFile.era_CurrentNeedsAssessmentid = n;
                    n.era_EvacuationFile._era_currentneedsassessmentid_value = n.era_needassessmentid;
                    return n.era_EvacuationFile;
                });
        }

        private async Task<string> CreateNote(EssContext essContext, string fileId, Note note, CancellationToken ct)
        {
            var file = essContext.era_evacuationfiles
                .Where(f => f.era_name == fileId).SingleOrDefault();
            if (file == null) throw new ArgumentException($"Evacuation file {fileId} not found");

            var newNote = mapper.Map<era_essfilenote>(note);
            newNote.era_essfilenoteid = Guid.NewGuid();
            essContext.AddToera_essfilenotes(newNote);
            essContext.AddLink(file, nameof(era_evacuationfile.era_era_evacuationfile_era_essfilenote_ESSFileID), newNote);
            essContext.SetLink(newNote, nameof(newNote.era_ESSFileID), file);

            if (newNote._era_essteamuserid_value.HasValue)
            {
                var user = essContext.era_essteamusers.Where(u => u.era_essteamuserid == newNote._era_essteamuserid_value).SingleOrDefault();
                if (user != null) essContext.AddLink(user, nameof(era_essteamuser.era_era_essteamuser_era_essfilenote_ESSTeamUser), newNote);
            }
            await essContext.SaveChangesAsync(ct);

            essContext.DetachAll();

            return newNote.era_essfilenoteid.ToString();
        }

        private async Task<string> UpdateNote(EssContext essContext, string fileId, Note note, CancellationToken ct)
        {
            var existingNote = await essContext.era_essfilenotes.ByKey(new Guid(note.Id)).GetValueAsync(ct);
            essContext.DetachAll();

            if (existingNote == null) throw new ArgumentException($"Evacuation file note {note.Id} not found");

            var updatedNote = mapper.Map<era_essfilenote>(note);

            updatedNote.era_essfilenoteid = existingNote.era_essfilenoteid;
            essContext.AttachTo(nameof(EssContext.era_essfilenotes), updatedNote);
            essContext.UpdateObject(updatedNote);

            await essContext.SaveChangesAsync(ct);

            essContext.DetachAll();

            return updatedNote.era_essfilenoteid.ToString();
        }
    }
}
