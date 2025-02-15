import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormControl,
  FormGroup,
  FormGroupDirective,
  NgForm,
  Validators
} from '@angular/forms';
import { ErrorStateMatcher } from '@angular/material/core';
import { CustomValidationService } from 'src/app/core/services/customValidation.service';

export class BcscCustomErrorMailMatcher implements ErrorStateMatcher {
  isErrorState(
    control: FormControl | null,
    form: FormGroupDirective | NgForm | null
  ): boolean {
    const isSubmitted = form && form.submitted;
    return (
      !!(
        control &&
        control.invalid &&
        (control.dirty || control.touched || isSubmitted)
      ) || control.parent.hasError('emailMatch')
    );
  }
}

@Component({
  selector: 'app-bcsc-invite-dialog',
  templateUrl: './bcsc-invite-dialog.component.html',
  styleUrls: ['./bcsc-invite-dialog.component.scss']
})
export class BcscInviteDialogComponent implements OnInit {
  @Output() outputEvent = new EventEmitter<string>();
  @Input() initDialog: string;
  emailFormGroup: FormGroup;
  showConfirm = false;
  emailMatcher = new BcscCustomErrorMailMatcher();
  hideForm = true;
  showError = false;

  constructor(
    private formBuilder: FormBuilder,
    private customValidation: CustomValidationService
  ) {}

  ngOnInit(): void {
    this.bcscEmailForm();
    this.emailFormGroup.get('email').valueChanges.subscribe((value) => {
      if (value.length > 0) {
        this.showConfirm = true;
      }
    });
    this.emailFormGroup.valueChanges.subscribe((formValue) => {
      this.showError = !this.emailFormGroup.valid;
    });
  }

  bcscEmailForm(): void {
    this.emailFormGroup = this.formBuilder.group(
      {
        email: [
          '',
          [
            Validators.email,
            this.customValidation.conditionalValidation(
              () =>
                !this.hideForm ||
                this.initDialog === null ||
                this.initDialog === '' ||
                this.initDialog === undefined,
              this.customValidation.whitespaceValidator()
            )
          ]
        ],
        confirmEmail: [
          '',
          [
            Validators.email,
            this.customValidation.conditionalValidation(
              () => !this.hideForm,
              this.customValidation.whitespaceValidator()
            )
          ]
        ]
      },
      {
        validators: [this.customValidation.confirmEmailValidator()]
      }
    );
  }

  /**
   * Returns the control of the form
   */
  get emailFormControl(): { [key: string]: AbstractControl } {
    return this.emailFormGroup.controls;
  }

  close() {
    this.outputEvent.emit('close');
  }

  send() {
    this.showError = !this.emailFormGroup.valid;
    if (!this.emailFormGroup.valid) {
      this.emailFormGroup.get('email').markAsTouched();
      this.emailFormGroup.get('confirmEmail').markAsTouched();
    } else if (this.emailFormGroup.get('email').value) {
      this.outputEvent.emit(this.emailFormGroup.get('email').value);
    } else if (this.hideForm) {
      this.outputEvent.emit(this.initDialog);
    }
  }

  openForm() {
    this.hideForm = false;
  }
}
