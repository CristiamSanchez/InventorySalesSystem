import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { SuppliersComponent } from './suppliers.component';
import { AuthService, SupplierResponse, SupplierService } from '@core';

describe('SuppliersComponent', () => {
  let mockSupplierService: {
    list: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    activate: ReturnType<typeof vi.fn>;
    deactivate: ReturnType<typeof vi.fn>;
  };
  let mockAuthService: {
    isAdmin: ReturnType<typeof vi.fn>;
  };

  const initialSuppliers: SupplierResponse[] = [
    {
      id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      name: 'Proveedor Mayorista Central S.A.',
      contactName: 'Carlos Mendoza',
      identificationNumber: 'J-30112233-1',
      email: 'ventas@central.com',
      phone: '+506 2555-1111',
      address: 'Cartago, Costa Rica',
      isActive: true,
      createdAtUtc: '2026-10-20T08:00:00Z'
    },
    {
      id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
      name: 'Importaciones del Caribe S.R.L.',
      contactName: null,
      identificationNumber: 'J-30998877-2',
      email: null,
      phone: null,
      address: null,
      isActive: false,
      createdAtUtc: '2026-10-21T09:00:00Z'
    }
  ];

  beforeEach(async () => {
    mockSupplierService = {
      list: vi.fn().mockReturnValue(of(initialSuppliers)),
      getById: vi.fn(),
      create: vi.fn(),
      update: vi.fn(),
      activate: vi.fn(),
      deactivate: vi.fn()
    };

    mockAuthService = {
      isAdmin: vi.fn().mockReturnValue(true)
    };

    await TestBed.configureTestingModule({
      imports: [SuppliersComponent],
      providers: [
        { provide: SupplierService, useValue: mockSupplierService },
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();
  });

  it('should create and load suppliers on init', () => {
    const fixture = TestBed.createComponent(SuppliersComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
    expect(mockSupplierService.list).toHaveBeenCalled();
    expect(component.suppliers().length).toBe(2);

    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Proveedor Mayorista Central S.A.');
    expect(rows[0].textContent).toContain('Carlos Mendoza');
    expect(rows[0].textContent).toContain('J-30112233-1');
    expect(rows[0].textContent).toContain('ventas@central.com');
    expect(rows[0].textContent).toContain('Activo');

    expect(rows[1].textContent).toContain('Importaciones del Caribe S.R.L.');
    expect(rows[1].textContent).toContain('Inactivo');
  });

  it('should filter suppliers by search term and status', () => {
    const fixture = TestBed.createComponent(SuppliersComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;

    // Filter by supplier company name
    component.searchTerm.set('Caribe');
    expect(component.filteredSuppliers().length).toBe(1);
    expect(component.filteredSuppliers()[0].name).toBe('Importaciones del Caribe S.R.L.');

    // Filter by contact name
    component.searchTerm.set('Carlos');
    expect(component.filteredSuppliers().length).toBe(1);
    expect(component.filteredSuppliers()[0].contactName).toBe('Carlos Mendoza');

    // Status filter: active only
    component.searchTerm.set('');
    component.selectedStatusFilter.set('active');
    expect(component.filteredSuppliers().length).toBe(1);
    expect(component.filteredSuppliers()[0].isActive).toBe(true);

    // Status filter: inactive only
    component.selectedStatusFilter.set('inactive');
    expect(component.filteredSuppliers().length).toBe(1);
    expect(component.filteredSuppliers()[0].isActive).toBe(false);

    // Reset filters
    component.resetFilters();
    expect(component.searchTerm()).toBe('');
    expect(component.selectedStatusFilter()).toBe('all');
    expect(component.filteredSuppliers().length).toBe(2);
  });

  it('should render empty state when no suppliers exist', () => {
    mockSupplierService.list.mockReturnValue(of([]));
    const fixture = TestBed.createComponent(SuppliersComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.suppliers().length).toBe(0);

    const emptyTitle = fixture.nativeElement.querySelector('.state-title');
    expect(emptyTitle?.textContent).toContain('No hay proveedores registrados');
  });

  it('should handle API loading error gracefully', () => {
    mockSupplierService.list.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500, statusText: 'Server Error' }))
    );
    const fixture = TestBed.createComponent(SuppliersComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.isLoading()).toBe(false);
    expect(component.errorMessage()).toBeTruthy();

    const alert = fixture.nativeElement.querySelector('.alert-danger');
    expect(alert).toBeTruthy();
  });

  it('should open create modal and validate required fields', () => {
    const fixture = TestBed.createComponent(SuppliersComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.openCreateModal();
    fixture.detectChanges();

    expect(component.isFormModalOpen()).toBe(true);
    expect(component.editingSupplier()).toBeNull();
    expect(component.form.valid).toBe(false); // name is required

    // Form invalid when name is empty
    component.onSaveSupplier();
    expect(component.form.get('name')?.touched).toBe(true);
    expect(mockSupplierService.create).not.toHaveBeenCalled();

    // Invalid email check
    component.form.patchValue({ name: 'Proveedor Test', email: 'correo-invalido' });
    expect(component.form.get('email')?.invalid).toBe(true);
    expect(component.form.valid).toBe(false);

    // Valid email
    component.form.patchValue({ email: 'valido@proveedor.com' });
    expect(component.form.valid).toBe(true);
  });

  it('should submit create form and add new supplier to list', () => {
    const newSupplier: SupplierResponse = {
      id: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
      name: 'Distribuciones Pacífica S.A.',
      contactName: 'Lucía Fernández',
      identificationNumber: 'J-31002233-4',
      email: 'pedidos@pacifica.com',
      phone: '8999-7777',
      address: 'Puntarenas',
      isActive: true,
      createdAtUtc: '2026-10-22T14:30:00Z'
    };

    mockSupplierService.create.mockReturnValue(of(newSupplier));
    const fixture = TestBed.createComponent(SuppliersComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.openCreateModal();
    component.form.setValue({
      name: 'Distribuciones Pacífica S.A.',
      contactName: 'Lucía Fernández',
      identificationNumber: 'J-31002233-4',
      email: 'pedidos@pacifica.com',
      phone: '8999-7777',
      address: 'Puntarenas'
    });

    component.onSaveSupplier();
    expect(mockSupplierService.create).toHaveBeenCalledWith({
      name: 'Distribuciones Pacífica S.A.',
      contactName: 'Lucía Fernández',
      identificationNumber: 'J-31002233-4',
      email: 'pedidos@pacifica.com',
      phone: '8999-7777',
      address: 'Puntarenas'
    });

    expect(component.suppliers().length).toBe(3);
    expect(component.suppliers()[0].name).toBe('Distribuciones Pacífica S.A.');
    expect(component.isFormModalOpen()).toBe(false);
    expect(component.successMessage()).toContain('Distribuciones Pacífica S.A.');
  });

  it('should open edit modal and save updated supplier', () => {
    const updatedSupplier: SupplierResponse = {
      ...initialSuppliers[0],
      name: 'Proveedor Mayorista Central Modificado',
      contactName: 'Carlos M. Mendoza',
      address: 'Cartago Centro'
    };

    mockSupplierService.update.mockReturnValue(of(updatedSupplier));
    const fixture = TestBed.createComponent(SuppliersComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.openEditModal(initialSuppliers[0]);
    expect(component.isFormModalOpen()).toBe(true);
    expect(component.editingSupplier()?.id).toBe(initialSuppliers[0].id);

    component.form.patchValue({
      name: 'Proveedor Mayorista Central Modificado',
      contactName: 'Carlos M. Mendoza',
      address: 'Cartago Centro'
    });

    component.onSaveSupplier();
    expect(mockSupplierService.update).toHaveBeenCalledWith(
      initialSuppliers[0].id,
      expect.objectContaining({
        name: 'Proveedor Mayorista Central Modificado',
        contactName: 'Carlos M. Mendoza',
        address: 'Cartago Centro'
      })
    );

    expect(component.suppliers()[0].name).toBe('Proveedor Mayorista Central Modificado');
    expect(component.suppliers()[0].contactName).toBe('Carlos M. Mendoza');
    expect(component.isFormModalOpen()).toBe(false);
    expect(component.successMessage()).toContain('actualizado con éxito');
  });

  it('should toggle supplier active status via activate/deactivate', () => {
    const deactivatedCentral: SupplierResponse = {
      ...initialSuppliers[0],
      isActive: false
    };
    mockSupplierService.deactivate.mockReturnValue(of(deactivatedCentral));

    const fixture = TestBed.createComponent(SuppliersComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;

    // Toggle active supplier -> calls deactivate
    component.onToggleActive(initialSuppliers[0]);
    expect(mockSupplierService.deactivate).toHaveBeenCalledWith(initialSuppliers[0].id);
    expect(component.suppliers()[0].isActive).toBe(false);
    expect(component.successMessage()).toContain('desactivado con éxito');

    // Toggle inactive supplier -> calls activate
    const activatedCaribe: SupplierResponse = {
      ...initialSuppliers[1],
      isActive: true
    };
    mockSupplierService.activate.mockReturnValue(of(activatedCaribe));

    component.onToggleActive(initialSuppliers[1]);
    expect(mockSupplierService.activate).toHaveBeenCalledWith(initialSuppliers[1].id);
    expect(component.suppliers()[1].isActive).toBe(true);
    expect(component.successMessage()).toContain('activado con éxito');
  });

  it('should hide mutation buttons when user is not admin', () => {
    mockAuthService.isAdmin.mockReturnValue(false);
    const fixture = TestBed.createComponent(SuppliersComponent);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.page-actions')).toBeNull();
    expect(root.querySelectorAll('.actions-group').length).toBe(0);
  });
});
