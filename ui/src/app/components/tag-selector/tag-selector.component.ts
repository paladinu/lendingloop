import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { SystemTag } from '../../models/system-tag.interface';
import { TagsService } from '../../services/tags.service';

@Component({
  selector: 'app-tag-selector',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './tag-selector.component.html',
  styleUrl: './tag-selector.component.css'
})
export class TagSelectorComponent implements OnInit, OnDestroy {
  @Input() selectedTags: string[] = [];
  @Output() selectedTagsChange = new EventEmitter<string[]>();

  availableTags: SystemTag[] = [];
  filteredTags: SystemTag[] = [];
  searchQuery: string = '';
  isLoading: boolean = false;
  errorMessage: string = '';
  validationMessage: string = '';

  private destroy$ = new Subject<void>();
  private readonly MAX_TAGS = 10;

  constructor(private tagsService: TagsService) {}

  ngOnInit(): void {
    this.loadTags();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadTags(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.tagsService.getAllTags()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (tags) => {
          this.availableTags = tags;
          this.filteredTags = tags;
          this.isLoading = false;
          this.errorMessage = '';
        },
        error: (error) => {
          console.error('Error loading tags:', error);
          this.errorMessage = 'Failed to load tags. Please try again.';
          this.isLoading = false;
        }
      });
  }

  retryLoadTags(): void {
    this.loadTags();
  }

  onSearchChange(): void {
    this.filteredTags = this.tagsService.searchTags(this.searchQuery, this.availableTags);
  }

  onTagSelect(tagName: string): void {
    if (this.selectedTags.includes(tagName)) {
      return; // Already selected
    }

    if (this.selectedTags.length >= this.MAX_TAGS) {
      this.validationMessage = `You can select up to ${this.MAX_TAGS} tags per item.`;
      setTimeout(() => this.validationMessage = '', 3000);
      return;
    }

    const newSelectedTags = [...this.selectedTags, tagName];
    this.selectedTags = newSelectedTags;
    this.selectedTagsChange.emit(newSelectedTags);
    this.validationMessage = '';
  }

  onTagRemove(tagName: string): void {
    const newSelectedTags = this.selectedTags.filter(tag => tag !== tagName);
    this.selectedTags = newSelectedTags;
    this.selectedTagsChange.emit(newSelectedTags);
    this.validationMessage = '';
  }

  isTagSelected(tagName: string): boolean {
    return this.selectedTags.includes(tagName);
  }

  getTagDisplayName(tagName: string): string {
    const tag = this.availableTags.find(t => t.name === tagName);
    return tag ? tag.displayName : tagName;
  }

  clearSearch(): void {
    this.searchQuery = '';
    this.filteredTags = this.availableTags;
  }
}
