import { Component, input } from '@angular/core';

@Component({
  selector: 'app-placeholder',
  standalone: true,
  templateUrl: './placeholder.component.html',
  styleUrl: './placeholder.component.scss'
})
export class PlaceholderComponent {
  readonly title = input.required<string>();
  readonly description = input('Manage records and workflows for your hospital.');
}
