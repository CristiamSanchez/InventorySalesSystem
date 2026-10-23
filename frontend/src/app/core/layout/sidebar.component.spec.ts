import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SidebarComponent } from './sidebar.component';

describe('SidebarComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SidebarComponent],
      providers: [provideRouter([])]
    }).compileComponents();
  });

  it('should create sidebar component', () => {
    const fixture = TestBed.createComponent(SidebarComponent);
    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
  });

  it('should render all 9 core navigation links', () => {
    const fixture = TestBed.createComponent(SidebarComponent);
    fixture.detectChanges();
    const links = fixture.nativeElement.querySelectorAll('.nav-link') as NodeListOf<HTMLAnchorElement>;
    expect(links.length).toBe(9);

    const paths = Array.from(links).map((link) => link.getAttribute('ng-reflect-router-link') || link.getAttribute('href'));
    expect(paths.some((p) => p?.includes('/dashboard'))).toBe(true);
    expect(paths.some((p) => p?.includes('/categories'))).toBe(true);
    expect(paths.some((p) => p?.includes('/products'))).toBe(true);
    expect(paths.some((p) => p?.includes('/customers'))).toBe(true);
    expect(paths.some((p) => p?.includes('/suppliers'))).toBe(true);
    expect(paths.some((p) => p?.includes('/inventory'))).toBe(true);
    expect(paths.some((p) => p?.includes('/sales'))).toBe(true);
    expect(paths.some((p) => p?.includes('/reports'))).toBe(true);
    expect(paths.some((p) => p?.includes('/audit'))).toBe(true);
  });
});
