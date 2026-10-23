import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { CustomersComponent } from './customers.component';
import { AuthService, CustomerResponse, CustomerService } from '@core';

describe('CustomersComponent', () => {
  let mockCustomerService: {
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

  const initialCustomers: CustomerResponse[] = [
    {
      id: '11111111-1111-1111-1111-111111111111',
      name: 'Corporación Alfa S.A.',
      identificationNumber: 'J-12345678-0',
      email: 'contacto@alfa.com',
      phone: '+506 2222-3333',
      address: 'San José, Costa Rica',
      isActive: true,
      createdAtUtc: '2026-10-20T10:00:00Z'
    },
    {
      id: '22222222-2222-2222-2222-222222222222',
      name: 'Distribuidora Beta Ltda.',
      identificationNumber: 'J-87654321-9',
      email: null,
      phone: null,
      address: null,
      isActive: false,
      createdAtUtc: '2026-10-21T11:00:00Z'
    }
  ];

  beforeEach(async () => {
    mockCustomerService = {
      list: vi.fn().mockReturnValue(of(initialCustomers)),
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
      imports: [CustomersComponent],
      providers: [
        { provide: CustomerService, useValue: mockCustomerService },
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();
  });

  it('should create and load customers on init', () => {
    const fixture = TestBed.createComponent(CustomersComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
    expect(mockCustomerService.list).toHaveBeenCalled();
    expect(component.customers().length).toBe(2);

    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Corporación Alfa S.A.');
    expect(rows[0].textContent).toContain('J-12345678-0');
    expect(rows[0].textContent).toContain('contacto@alfa.com');
    expect(rows[0].textContent).toContain('Activo');

    expect(rows[1].textContent).toContain('Distribuidora Beta Ltda.');
    expect(rows[1].textContent).toContain('Inactivo');
  });

  it('should filter customers by search term and status', () => {
    const fixture = TestBed.createComponent(CustomersComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;

    // Filter by name
    component.searchTerm.set('Alfa');
    expect(component.filteredCustomers().length).toBe(1);
    expect(component.filteredCustomers()[0].name).toBe('Corporación Alfa S.A.');

    // Filter by identification number
    component.searchTerm.set('87654321');
    expect(component.filteredCustomers().length).toBe(1);
    expect(component.filteredCustomers()[0].name).toBe('Distribuidora Beta Ltda.');

    // Status filter: active only
    component.searchTerm.set('');
    component.selectedStatusFilter.set('active');
    expect(component.filteredCustomers().length).toBe(1);
    expect(component.filteredCustomers()[0].isActive).toBe(true);

    // Status filter: inactive only
    component.selectedStatusFilter.set('inactive');
    expect(component.filteredCustomers().length).toBe(1);
    expect(component.filteredCustomers()[0].isActive).toBe(false);

    // Reset filters
    component.resetFilters();
    expect(component.searchTerm()).toBe('');
    expect(component.selectedStatusFilter()).toBe('all');
    expect(component.filteredCustomers().length).toBe(2);
  });

  it('should render empty state when no customers exist', () => {
    mockCustomerService.list.mockReturnValue(of([]));
    const fixture = TestBed.createComponent(CustomersComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.customers().length).toBe(0);

    const emptyTitle = fixture.nativeElement.querySelector('.state-title');
    expect(emptyTitle?.textContent).toContain('No hay clientes registrados');
  });

  it('should handle API loading error gracefully', () => {
    mockCustomerService.list.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500, statusText: 'Server Error' }))
    );
    const fixture = TestBed.createComponent(CustomersComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.isLoading()).toBe(false);
    expect(component.errorMessage()).toBeTruthy();

    const alert = fixture.nativeElement.querySelector('.alert-danger');
    expect(alert).toBeTruthy();
  });

  it('should open create modal and validate required fields', () => {
    const fixture = TestBed.createComponent(CustomersComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.openCreateModal();
    fixture.detectChanges();

    expect(component.isFormModalOpen()).toBe(true);
    expect(component.editingCustomer()).toBeNull();
    expect(component.form.valid).toBe(false); // name is required

    // Form invalid when name is empty
    component.onSaveCustomer();
    expect(component.form.get('name')?.touched).toBe(true);
    expect(mockCustomerService.create).not.toHaveBeenCalled();

    // Invalid email check
    component.form.patchValue({ name: 'Cliente Prueba', email: 'correo-invalido' });
    expect(component.form.get('email')?.invalid).toBe(true);
    expect(component.form.valid).toBe(false);

    // Valid email
    component.form.patchValue({ email: 'valido@prueba.com' });
    expect(component.form.valid).toBe(true);
  });

  it('should submit create form and add new customer to list', () => {
    const newCustomer: CustomerResponse = {
      id: '33333333-3333-3333-3333-333333333333',
      name: 'Nuevo Cliente S.A.',
      identificationNumber: 'J-55555555-5',
      email: 'nuevo@cliente.com',
      phone: '8888-7777',
      address: 'Heredia',
      isActive: true,
      createdAtUtc: '2026-10-22T14:00:00Z'
    };

    mockCustomerService.create.mockReturnValue(of(newCustomer));
    const fixture = TestBed.createComponent(CustomersComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.openCreateModal();
    component.form.setValue({
      name: 'Nuevo Cliente S.A.',
      identificationNumber: 'J-55555555-5',
      email: 'nuevo@cliente.com',
      phone: '8888-7777',
      address: 'Heredia'
    });

    component.onSaveCustomer();
    expect(mockCustomerService.create).toHaveBeenCalledWith({
      name: 'Nuevo Cliente S.A.',
      identificationNumber: 'J-55555555-5',
      email: 'nuevo@cliente.com',
      phone: '8888-7777',
      address: 'Heredia'
    });

    expect(component.customers().length).toBe(3);
    expect(component.customers()[0].name).toBe('Nuevo Cliente S.A.');
    expect(component.isFormModalOpen()).toBe(false);
    expect(component.successMessage()).toContain('Nuevo Cliente S.A.');
  });

  it('should open edit modal and save updated customer', () => {
    const updatedCustomer: CustomerResponse = {
      ...initialCustomers[0],
      name: 'Corporación Alfa Modificada',
      address: 'Cartago'
    };

    mockCustomerService.update.mockReturnValue(of(updatedCustomer));
    const fixture = TestBed.createComponent(CustomersComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.openEditModal(initialCustomers[0]);
    expect(component.isFormModalOpen()).toBe(true);
    expect(component.editingCustomer()?.id).toBe(initialCustomers[0].id);

    component.form.patchValue({
      name: 'Corporación Alfa Modificada',
      address: 'Cartago'
    });

    component.onSaveCustomer();
    expect(mockCustomerService.update).toHaveBeenCalledWith(
      initialCustomers[0].id,
      expect.objectContaining({ name: 'Corporación Alfa Modificada', address: 'Cartago' })
    );

    expect(component.customers()[0].name).toBe('Corporación Alfa Modificada');
    expect(component.isFormModalOpen()).toBe(false);
    expect(component.successMessage()).toContain('actualizado con éxito');
  });

  it('should toggle customer active status via activate/deactivate', () => {
    const deactivatedAlfa: CustomerResponse = {
      ...initialCustomers[0],
      isActive: false
    };
    mockCustomerService.deactivate.mockReturnValue(of(deactivatedAlfa));

    const fixture = TestBed.createComponent(CustomersComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;

    // Toggle active customer -> calls deactivate
    component.onToggleActive(initialCustomers[0]);
    expect(mockCustomerService.deactivate).toHaveBeenCalledWith(initialCustomers[0].id);
    expect(component.customers()[0].isActive).toBe(false);
    expect(component.successMessage()).toContain('desactivado con éxito');

    // Toggle inactive customer -> calls activate
    const activatedBeta: CustomerResponse = {
      ...initialCustomers[1],
      isActive: true
    };
    mockCustomerService.activate.mockReturnValue(of(activatedBeta));

    component.onToggleActive(initialCustomers[1]);
    expect(mockCustomerService.activate).toHaveBeenCalledWith(initialCustomers[1].id);
    expect(component.customers()[1].isActive).toBe(true);
    expect(component.successMessage()).toContain('activado con éxito');
  });

  it('should hide mutation buttons when user is not admin', () => {
    mockAuthService.isAdmin.mockReturnValue(false);
    const fixture = TestBed.createComponent(CustomersComponent);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.page-actions')).toBeNull();
    expect(root.querySelectorAll('.actions-group').length).toBe(0);
  });
});
