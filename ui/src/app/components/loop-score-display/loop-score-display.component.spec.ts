import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LoopScoreDisplayComponent } from './loop-score-display.component';
import { DebugElement } from '@angular/core';
import { By } from '@angular/platform-browser';

describe('LoopScoreDisplayComponent', () => {
    let component: LoopScoreDisplayComponent;
    let fixture: ComponentFixture<LoopScoreDisplayComponent>;
    let compiled: DebugElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [LoopScoreDisplayComponent]
        }).compileComponents();

        fixture = TestBed.createComponent(LoopScoreDisplayComponent);
        component = fixture.componentInstance;
        compiled = fixture.debugElement;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should display score value correctly', () => {
        //arrange
        component.score = 15;

        //act
        fixture.detectChanges();

        //assert
        const scoreValue = compiled.query(By.css('.score-value'));
        expect(scoreValue.nativeElement.textContent.trim()).toBe('15');
    });

    it('should apply correct size class for small', () => {
        //arrange
        component.size = 'small';

        //act
        fixture.detectChanges();

        //assert
        const badge = compiled.query(By.css('.loop-score-badge'));
        expect(badge.nativeElement.classList.contains('size-small')).toBe(true);
    });

    it('should apply correct size class for medium', () => {
        //arrange
        component.size = 'medium';

        //act
        fixture.detectChanges();

        //assert
        const badge = compiled.query(By.css('.loop-score-badge'));
        expect(badge.nativeElement.classList.contains('size-medium')).toBe(true);
    });

    it('should apply correct size class for large', () => {
        //arrange
        component.size = 'large';

        //act
        fixture.detectChanges();

        //assert
        const badge = compiled.query(By.css('.loop-score-badge'));
        expect(badge.nativeElement.classList.contains('size-large')).toBe(true);
    });

    it('should show 0 for new users', () => {
        //arrange
        component.score = 0;

        //act
        fixture.detectChanges();

        //assert
        const scoreValue = compiled.query(By.css('.score-value'));
        expect(scoreValue.nativeElement.textContent.trim()).toBe('0');
    });

    it('should set aria-label correctly', () => {
        //arrange
        component.score = 10;

        //act
        fixture.detectChanges();

        //assert
        const badge = compiled.query(By.css('.loop-score-badge'));
        expect(badge.nativeElement.getAttribute('aria-label')).toBe('LoopScore: 10');
    });

    it('should display star icon', () => {
        //act
        fixture.detectChanges();

        //assert
        const icon = compiled.query(By.css('.score-icon'));
        expect(icon.nativeElement.textContent).toBe('⭐');
    });
});
