import { Component, ElementRef, EventEmitter, HostListener, Input, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-filter-dropdown',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './filter-dropdown.component.html',
  styles: ':host { display: inline-block; position: relative; }',
})
export class FilterDropdownComponent {
  @Input() label = '';
  @Input() options: string[] = [];
  @Input() selected = new Set<string>();
  @Input() searchable = false;
  @Output() selectedChange = new EventEmitter<Set<string>>();

  open = signal(false);
  search = signal('');

  // Métodos comuns (não computed()) porque dependem de @Input, não de signals —
  // computed() não reagiria a mudanças vindas do componente pai.
  filteredOptions(): string[] {
    const term = this.search().trim().toLowerCase();
    return term ? this.options.filter((o) => o.toLowerCase().includes(term)) : this.options;
  }

  // Segue a semântica do Azure DevOps: nenhuma opção marcada = sem filtro (mostra tudo).
  isFiltering(): boolean {
    return this.selected.size > 0;
  }

  constructor(private readonly elementRef: ElementRef<HTMLElement>) {}

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.open() && !this.elementRef.nativeElement.contains(event.target as Node)) {
      this.open.set(false);
    }
  }

  toggleOpen(): void {
    this.open.update((o) => !o);
  }

  toggleOption(option: string, checked: boolean): void {
    const next = new Set(this.selected);
    checked ? next.add(option) : next.delete(option);
    this.selectedChange.emit(next);
  }

  clear(): void {
    this.selectedChange.emit(new Set());
    this.search.set('');
  }
}
