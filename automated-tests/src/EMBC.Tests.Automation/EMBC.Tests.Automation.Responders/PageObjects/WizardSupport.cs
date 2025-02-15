﻿using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using Protractor;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMBC.Tests.Automation.Responders.PageObjects
{
    internal class WizardSupport : WizardPageObjectBase
    {
        //ELEMENTS:
        private By viewSupportsMainTable = By.CssSelector("table[role='table']");
        private By selectSupportFormTypeSelect = By.CssSelector("mat-select[formcontrolname='type']");

        private By selectWinterConditionRadioGroup = By.CssSelector("mat-radio-group[formcontrolname='extremeWinterConditions']");

        private By supportDetailsFormNbrDaysSelect = By.CssSelector("mat-select[formcontrolname='noOfDays']");
        private By supportDetailsFormAllMembersCheckbox = By.Id("allMembers");
        private By supportDetailsFormtotalAmountInput = By.CssSelector("input[formcontrolname='userTotalAmount']");

        private By supportDetailsFormPaperSupportNbrInput = By.CssSelector("input[formcontrolname='paperSupportNumber']");
        private By supportDetailsFormPaperCompletedOnInput = By.CssSelector("input[formcontrolname='paperCompletedOn']");
        private By supportDetailsFormValidFromDateInput = By.CssSelector("input[formcontrolname='fromDate']");
        private By supportDetailsFormValidFromTimeInput = By.CssSelector("input[formcontrolname='fromTime']");
        private By supportDetailsFormValidToDateInput = By.CssSelector("input[formcontrolname='toDate']");
        private By supportDetailsFormValidToTimeInput = By.CssSelector("input[formcontrolname='toTime']");
        private By supportDetailsFormNbrOfRoomsSelect = By.CssSelector("mat-select[formcontrolname='noOfRooms']");

        private By supportDeliveryReferralCard = By.Id("referralCard");
        private By supportDeliveryInteracCard = By.Id("interacCard");
        
        private By supportDeliveryResposibleSelect = By.CssSelector("mat-select[formcontrolname='issuedTo']");
        private By supportDeliveryResponsibleNameInput = By.CssSelector("input[formcontrolname='name']");
        private By supportDeliverySupplierInput = By.CssSelector("input[formcontrolname='supplier']");
        private By supportDeliverySupplierSelect = By.XPath("//div[@role='listbox']/mat-option[1]");

        private By supportDeliveryNotificationSelect = By.CssSelector("mat-select[formcontrolname='notificationPreference']");
        private By supportDeliveryEmailMobileNotOption = By.XPath("//mat-option/span[contains(text(), 'Email & Mobile')]/ancestor::mat-option");
        private By supportDeliveryEmailInput = By.CssSelector("input[formcontrolname='notificationEmail']");
        private By supportDeliveryEmailConfirmInput = By.CssSelector("input[formcontrolname='notificationConfirmEmail']");
        private By supportDeliveryMobileInput = By.CssSelector("input[formcontrolname='notificationMobile']");
        private By supportDeliveryMobileConfirmInput = By.CssSelector("input[formcontrolname='notificationConfirmMobile']");

        private By processSupportFormCertificateCheckBox = By.Id("processDraftCert");
        private By viewSupportFormSupportStatus = By.XPath("//table[@role='table']/tbody/tr/td[7]");

        public WizardSupport(IWebDriver webDriver) : base(webDriver)
        { }

        public void WizardAddSupport()
        {
            if (webDriver.FindElements(viewSupportsMainTable).Count > 0)
            {
                ButtonElement("Add Supports");
            }
            else
            {
                ButtonElement("+ Add Supports");
            }
            
        }

        public void WizardSelectSupportForm(string supportType)
        {
            Wait();

            webDriver.FindElement(selectSupportFormTypeSelect).SendKeys(supportType);
            ButtonElement("Next - Support Details");
        }

        public void SupportClothingDetailsForm(string totalAmount)
        {
            Wait();

            FocusAndClick(supportDetailsFormAllMembersCheckbox);
            var winterClothingElement = webDriver.FindElement(selectWinterConditionRadioGroup);
            ChooseRandomOption(winterClothingElement, "extremeWinterConditions");
            webDriver.FindElement(supportDetailsFormtotalAmountInput).SendKeys(totalAmount);
            ButtonElement("Next - Support Delivery");
        }

        public void SupportFoodDetailsForm(string nbrOfDays, string mealValue)
        {
            Wait();

            webDriver.FindElement(supportDetailsFormNbrDaysSelect).SendKeys(nbrOfDays);
            FocusAndClick(supportDetailsFormAllMembersCheckbox);
            webDriver.FindElement(supportDetailsFormtotalAmountInput).SendKeys(mealValue);
            ButtonElement("Next - Support Delivery");
        }

        public void SupportPaperBasedHotelDetailsForm(string interviewer, string interviewerInitials, string completedDate, string validToDate, string validTime, string nbrRooms)
        {
            var supportNbr = Randomizer("D8");

            Wait();

            webDriver.FindElement(supportDetailsFormPaperSupportNbrInput).SendKeys(supportNbr);
            webDriver.FindElement(personFormFirstNameInput).SendKeys(interviewer);
            webDriver.FindElement(wizardInterviewerInitialsInput).SendKeys(interviewerInitials);
            webDriver.FindElement(supportDetailsFormPaperCompletedOnInput).SendKeys(completedDate);
            webDriver.FindElement(supportDetailsFormValidFromDateInput).SendKeys(completedDate);
            webDriver.FindElement(supportDetailsFormValidFromTimeInput).SendKeys(validTime);
            webDriver.FindElement(supportDetailsFormValidToDateInput).SendKeys(validToDate);
            webDriver.FindElement(supportDetailsFormValidToTimeInput).SendKeys(validTime);

            FocusAndClick(supportDetailsFormAllMembersCheckbox);
            webDriver.FindElement(supportDetailsFormNbrOfRoomsSelect).SendKeys(nbrRooms);
            ButtonElement("Next - Support Delivery");
        }

        public void SupportReferralDeliveryForm(string responsibleName)
        {
            var responsible = "Someone else";

            webDriver.FindElement(supportDeliveryReferralCard).Click();
            Wait();

            webDriver.FindElement(supportDeliveryResposibleSelect).SendKeys(responsible);
            Wait();

            webDriver.FindElement(supportDeliveryResponsibleNameInput).SendKeys(responsibleName);
            webDriver.FindElement(supportDeliverySupplierInput).Click();
            FocusAndClick(supportDeliverySupplierSelect);

            ButtonElement("Next - Save Support");
        }

        public void SupportInteracDeliveryForm(string email, string phone)
        {
            var js = (IJavaScriptExecutor)webDriver;

            webDriver.FindElement(supportDeliveryInteracCard).Click();
            Wait();

            var selectNotificationElement = webDriver.FindElement(supportDeliveryNotificationSelect);
            js.ExecuteScript("arguments[0].scrollIntoView();", selectNotificationElement);
            
            Wait();
            selectNotificationElement.Click();

            webDriver.FindElement(supportDeliveryEmailMobileNotOption).Click();
            Wait();

            webDriver.FindElement(supportDeliveryEmailInput).SendKeys(email);
            webDriver.FindElement(supportDeliveryEmailConfirmInput).SendKeys(email);
            webDriver.FindElement(supportDeliveryMobileInput).SendKeys(phone);
            webDriver.FindElement(supportDeliveryMobileConfirmInput).SendKeys(phone);

            ButtonElement("Next - Save Support");
        }

        public void SuccessSupportPopUp()
        {
            Wait();
            ButtonElement("Close");
        }

        public void WizardProcessDraftSupports()
        {
            Wait();
            ButtonElement("Process Draft Support/s");
            
        }

        public void WizardOnlineProcessSupportsForm()
        {
            FocusAndClick(processSupportFormCertificateCheckBox);
            ButtonElement("Process Support/s");
            ButtonElement("Proceed");
        }

        public void WizardPaperBasedProcessSupportsForm()
        {
            ButtonElement("Process Support/s");
            ButtonElement("Proceed");

            
        }

        public void CancelPrintOut()
        {

            //Thread.Sleep(10000);

            string currentHandle = webDriver.CurrentWindowHandle;
            ReadOnlyCollection<string> originalHandles = webDriver.WindowHandles;


            WebDriverWait wait = new WebDriverWait(webDriver, TimeSpan.FromSeconds(60));
            string popupWindowHandle = wait.Until<string>((d) =>
            {
                string foundHandle = null;

                // Subtract out the list of known handles. In the case of a single
                // popup, the newHandles list will only have one value.
                List<string> newHandles = webDriver.WindowHandles.Except(originalHandles).ToList();
                if (newHandles.Count > 0)
                {
                    foundHandle = newHandles[0];
                }

                return foundHandle;
            });
            //w.Until(ExpectedConditions.ElementExists(webDriver.WindowHandles.Count > 1);

            var driver = webDriver is NgWebDriver ngDriver
               ? ngDriver.WrappedDriver
               : webDriver;

            //if (driver.WindowHandles.Count > 1)
            //{
                driver.SwitchTo().Window(popupWindowHandle);

                IWebElement element = driver.FindElement(By.TagName("print-preview-app"));
                element.SendKeys(Keys.Tab);
                element.SendKeys(Keys.Enter);

                driver.SwitchTo().Window(currentHandle);
            //}
            //else
            //{
            //    driver.SwitchTo().Window(driver.WindowHandles[1]);
            //    Actions action = new Actions(driver);
            //    action.SendKeys(Keys.Escape);
            //}

                
        }

        // ASSERT FUNCTIONS
        public string GetSupportStatus()
        {
            Wait();
            return webDriver.FindElement(viewSupportFormSupportStatus).Text;
        }
    }
}
