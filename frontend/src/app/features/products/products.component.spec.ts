import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { ProductsComponent } from './products.component';
import { AuthService, CategoryResponse, CategoryService, ProductResponse, ProductService } from '@core';

describe('ProductsComponent', () => {
  let mockProductService: {
    list: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
  };
  let mockCategoryService: {
    list: ReturnType<typeof vi.fn>;
  };
  let mockAuthService: {
    isAdmin: ReturnType<typeof vi.fn>;
  };

  const initialCategories: CategoryResponse[] = [
    {
      id: '11111111-1111-1111-1111-111111111111',
      name: 'Bebidas',
      isActive: true,
      products: []
    },
    {
      id: '22222222-2222-2222-2222-222222222222',
      name: 'Snacks',
      isActive: true,
      products: []
    }
  ];

  const initialProducts: ProductResponse[] = [
    {
      identifier: 'BEB-01',
      name: 'Agua Mineral 500ml',
      categoryId: '11111111-1111-1111-1111-111111111111',
      categoryName: 'Bebidas',
      isActive: true
    },
    {
      identifier: 'SNK-01',
      name: 'Papas Fritas 100g',
      categoryId: '22222222-2222-2222-2222-222222222222',
      categoryName: 'Snacks',
      isActive: false
    }
  ];

  beforeEach(async () => {
    mockProductService = {
      list: vi.fn().mockReturnValue(of(initialProducts)),
      create: vi.fn(),
      update: vi.fn()
    };

    mockCategoryService = {
      list: vi.fn().mockReturnValue(of(initialCategories))
    };

    mockAuthService = {
      isAdmin: vi.fn().mockReturnValue(true)
    };

    await TestBed.configureTestingModule({
      imports: [ProductsComponent],
      providers: [
        { provide: ProductService, useValue: mockProductService },
        { provide: CategoryService, useValue: mockCategoryService },
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();
  });

  it('should create and load products and categories on init', () => {
    const fixture = TestBed.createComponent(ProductsComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
    expect(mockProductService.list).toHaveBeenCalled();
    expect(mockCategoryService.list).toHaveBeenCalled();
    expect(component.products().length).toBe(2);
    expect(component.categories().length).toBe(2);

    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('BEB-01');
    expect(rows[0].textContent).toContain('Agua Mineral 500ml');
    expect(rows[0].textContent).toContain('Bebidas');
    expect(rows[0].textContent).toContain('Activo');
  });

  it('should reload products when category, status, or search filters change', () => {
    const fixture = TestBed.createComponent(ProductsComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.selectedCategoryFilter.set('11111111-1111-1111-1111-111111111111');
    component.selectedStatusFilter.set('active');
    component.searchTerm.set('Agua');

    component.loadProducts();

    expect(mockProductService.list).toHaveBeenCalledWith({
      categoryId: '11111111-1111-1111-1111-111111111111',
      isActive: true,
      searchTerm: 'Agua'
    });
  });

  it('should display empty state when product list is empty', () => {
    mockProductService.list.mockReturnValue(of([]));

    const fixture = TestBed.createComponent(ProductsComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.state-title')?.textContent).toContain('No hay productos registrados');
  });

  it('should open create modal and call product create on submit', () => {
    const fixture = TestBed.createComponent(ProductsComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.openCreateModal();

    expect(component.isFormModalOpen()).toBe(true);
    expect(component.editingProduct()).toBeNull();
    expect(component.form.controls.identifier.enabled).toBe(true);

    component.form.setValue({
      identifier: 'BEB-02',
      name: 'Jugo Natural',
      categoryId: '11111111-1111-1111-1111-111111111111',
      isActive: true
    });

    const created: ProductResponse = {
      identifier: 'BEB-02',
      name: 'Jugo Natural',
      categoryId: '11111111-1111-1111-1111-111111111111',
      categoryName: 'Bebidas',
      isActive: true
    };
    mockProductService.create.mockReturnValue(of(created));

    component.onSaveProduct();

    expect(mockProductService.create).toHaveBeenCalledWith({
      identifier: 'BEB-02',
      name: 'Jugo Natural',
      categoryId: '11111111-1111-1111-1111-111111111111'
    });
    expect(component.isFormModalOpen()).toBe(false);
    expect(component.products().some((p) => p.identifier === 'BEB-02')).toBe(true);
  });

  it('should open edit modal with disabled identifier and update product on submit', () => {
    const fixture = TestBed.createComponent(ProductsComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    const target = initialProducts[0];
    component.openEditModal(target);

    expect(component.isFormModalOpen()).toBe(true);
    expect(component.editingProduct()).toEqual(target);
    expect(component.form.controls.identifier.disabled).toBe(true);
    expect(component.form.controls.name.value).toBe('Agua Mineral 500ml');

    component.form.controls.name.setValue('Agua Mineral Purificada');
    component.form.controls.isActive.setValue(false);

    const updated: ProductResponse = {
      ...target,
      name: 'Agua Mineral Purificada',
      isActive: false
    };
    mockProductService.update.mockReturnValue(of(updated));

    component.onSaveProduct();

    expect(mockProductService.update).toHaveBeenCalledWith('BEB-01', {
      name: 'Agua Mineral Purificada',
      categoryId: target.categoryId,
      isActive: false
    });
    expect(component.isFormModalOpen()).toBe(false);
    const inList = component.products().find((p) => p.identifier === 'BEB-01');
    expect(inList?.name).toBe('Agua Mineral Purificada');
    expect(inList?.isActive).toBe(false);
  });

  it('should toggle product active status using productService.update', () => {
    const fixture = TestBed.createComponent(ProductsComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    const target = initialProducts[0]; // isActive = true

    const updated: ProductResponse = {
      ...target,
      isActive: false
    };
    mockProductService.update.mockReturnValue(of(updated));

    component.toggleProductStatus(target);

    expect(mockProductService.update).toHaveBeenCalledWith('BEB-01', {
      name: target.name,
      categoryId: target.categoryId,
      isActive: false
    });
    const inList = component.products().find((p) => p.identifier === 'BEB-01');
    expect(inList?.isActive).toBe(false);
  });

  it('should display error message when loading products fails', () => {
    const error = new HttpErrorResponse({ status: 500 });
    mockProductService.list.mockReturnValue(throwError(() => error));

    const fixture = TestBed.createComponent(ProductsComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.errorMessage()).toContain('error en el servidor');
    expect(fixture.nativeElement.querySelector('.alert-danger')).toBeTruthy();
  });

  it('should hide mutation buttons when user is not admin', () => {
    mockAuthService.isAdmin.mockReturnValue(false);

    const fixture = TestBed.createComponent(ProductsComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.btn-primary')).toBeFalsy();
    expect(compiled.querySelector('.badge-neutral')?.textContent).toContain('Modo Consulta');
    expect(compiled.querySelector('.actions-group')).toBeFalsy();
  });
});
