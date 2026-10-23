import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { CategoriesComponent } from './categories.component';
import { AuthService, CategoryResponse, CategoryService } from '@core';

describe('CategoriesComponent', () => {
  let mockCategoryService: {
    list: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
  };
  let mockAuthService: {
    isAdmin: ReturnType<typeof vi.fn>;
  };

  const initialCategories: CategoryResponse[] = [
    {
      id: '11111111-1111-1111-1111-111111111111',
      name: 'Bebidas',
      isActive: true,
      products: [{ identifier: 'BEB-01', name: 'Agua', isActive: true }]
    },
    {
      id: '22222222-2222-2222-2222-222222222222',
      name: 'Golosinas',
      isActive: false,
      products: []
    }
  ];

  beforeEach(async () => {
    mockCategoryService = {
      list: vi.fn().mockReturnValue(of(initialCategories)),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn()
    };

    mockAuthService = {
      isAdmin: vi.fn().mockReturnValue(true)
    };

    await TestBed.configureTestingModule({
      imports: [CategoriesComponent],
      providers: [
        { provide: CategoryService, useValue: mockCategoryService },
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();
  });

  it('should create and load categories on init', () => {
    const fixture = TestBed.createComponent(CategoriesComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
    expect(mockCategoryService.list).toHaveBeenCalled();
    expect(component.categories().length).toBe(2);
    expect(component.totalCount()).toBe(2);
    expect(component.activeCount()).toBe(1);
    expect(component.inactiveCount()).toBe(1);

    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Bebidas');
    expect(rows[1].textContent).toContain('Golosinas');
  });

  it('should filter categories based on search input', () => {
    const fixture = TestBed.createComponent(CategoriesComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.searchTerm.set('beb');

    expect(component.filteredCategories().length).toBe(1);
    expect(component.filteredCategories()[0].name).toBe('Bebidas');

    component.searchTerm.set('xyz');
    expect(component.filteredCategories().length).toBe(0);
  });

  it('should display empty state when no categories exist', () => {
    mockCategoryService.list.mockReturnValue(of([]));

    const fixture = TestBed.createComponent(CategoriesComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.state-title')?.textContent).toContain('No hay categorías registradas');
  });

  it('should open create modal and call create service on submit', () => {
    const fixture = TestBed.createComponent(CategoriesComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.openCreateModal();

    expect(component.isFormModalOpen()).toBe(true);
    expect(component.editingCategory()).toBeNull();

    component.form.controls.name.setValue('Limpieza');

    const created: CategoryResponse = {
      id: '33333333-3333-3333-3333-333333333333',
      name: 'Limpieza',
      isActive: true,
      products: []
    };
    mockCategoryService.create.mockReturnValue(of(created));

    component.onSaveCategory();

    expect(mockCategoryService.create).toHaveBeenCalledWith({ name: 'Limpieza' });
    expect(component.isFormModalOpen()).toBe(false);
    expect(component.categories().some((c) => c.name === 'Limpieza')).toBe(true);
  });

  it('should open edit modal and call update service on submit', () => {
    const fixture = TestBed.createComponent(CategoriesComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    const target = initialCategories[0];
    component.openEditModal(target);

    expect(component.isFormModalOpen()).toBe(true);
    expect(component.editingCategory()).toEqual(target);
    expect(component.form.controls.name.value).toBe('Bebidas');

    component.form.controls.name.setValue('Bebidas y Jugos');
    component.form.controls.isActive.setValue(false);

    const updated: CategoryResponse = {
      ...target,
      name: 'Bebidas y Jugos',
      isActive: false
    };
    mockCategoryService.update.mockReturnValue(of(updated));

    component.onSaveCategory();

    expect(mockCategoryService.update).toHaveBeenCalledWith(target.id, {
      name: 'Bebidas y Jugos',
      isActive: false
    });
    expect(component.isFormModalOpen()).toBe(false);
    const inList = component.categories().find((c) => c.id === target.id);
    expect(inList?.name).toBe('Bebidas y Jugos');
    expect(inList?.isActive).toBe(false);
  });

  it('should open delete modal and call delete service on confirmation', () => {
    const fixture = TestBed.createComponent(CategoriesComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    const target = initialCategories[1];
    component.openDeleteModal(target);

    expect(component.isDeleteModalOpen()).toBe(true);
    expect(component.categoryToDelete()).toEqual(target);

    mockCategoryService.delete.mockReturnValue(of(void 0));
    component.onConfirmDelete();

    expect(mockCategoryService.delete).toHaveBeenCalledWith(target.id);
    expect(component.isDeleteModalOpen()).toBe(false);
    expect(component.categories().some((c) => c.id === target.id)).toBe(false);
  });

  it('should display error message when loading categories fails', () => {
    const error = new HttpErrorResponse({ status: 500 });
    mockCategoryService.list.mockReturnValue(throwError(() => error));

    const fixture = TestBed.createComponent(CategoriesComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.errorMessage()).toContain('error en el servidor');
    expect(fixture.nativeElement.querySelector('.alert-danger')).toBeTruthy();
  });

  it('should hide mutation buttons when user is not admin', () => {
    mockAuthService.isAdmin.mockReturnValue(false);

    const fixture = TestBed.createComponent(CategoriesComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.btn-primary')).toBeFalsy();
    expect(compiled.querySelector('.badge-neutral')?.textContent).toContain('Modo Consulta');
    expect(compiled.querySelector('.actions-group')).toBeFalsy();
  });
});
