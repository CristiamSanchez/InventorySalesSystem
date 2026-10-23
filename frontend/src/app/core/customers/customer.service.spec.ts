import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CustomerService } from './customer.service';
import {
  CreateCustomerRequest,
  CustomerResponse,
  UpdateCustomerRequest
} from './customer.models';

describe('CustomerService', () => {
  let service: CustomerService;
  let httpTesting: HttpTestingController;

  const mockCustomers: CustomerResponse[] = [
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

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CustomerService, provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(CustomerService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should retrieve all customers via GET /api/customers without query params', () => {
    let result: CustomerResponse[] = [];
    service.list().subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/customers');
    expect(req.request.method).toBe('GET');
    req.flush(mockCustomers);

    expect(result.length).toBe(2);
    expect(result[0].name).toBe('Corporación Alfa S.A.');
  });

  it('should retrieve filtered customers with query params via GET /api/customers', () => {
    let result: CustomerResponse[] = [];
    service.list({ isActive: true, searchTerm: 'Alfa' }).subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne((r) =>
      r.url === '/api/customers' &&
      r.params.get('isActive') === 'true' &&
      r.params.get('searchTerm') === 'Alfa'
    );
    expect(req.request.method).toBe('GET');
    req.flush([mockCustomers[0]]);

    expect(result.length).toBe(1);
    expect(result[0].id).toBe(mockCustomers[0].id);
  });

  it('should retrieve single customer by ID via GET /api/customers/{id}', () => {
    let result: CustomerResponse | null = null;
    service.getById('11111111-1111-1111-1111-111111111111').subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/customers/11111111-1111-1111-1111-111111111111');
    expect(req.request.method).toBe('GET');
    req.flush(mockCustomers[0]);

    expect(result).toEqual(mockCustomers[0]);
  });

  it('should create customer via POST /api/customers', () => {
    const createReq: CreateCustomerRequest = {
      name: 'Comercial Gamma',
      identificationNumber: 'J-99999999-9',
      email: 'gamma@comercial.com',
      phone: '8888-9999',
      address: 'Heredia'
    };

    const createdResponse: CustomerResponse = {
      id: '33333333-3333-3333-3333-333333333333',
      name: createReq.name,
      identificationNumber: createReq.identificationNumber ?? null,
      email: createReq.email ?? null,
      phone: createReq.phone ?? null,
      address: createReq.address ?? null,
      isActive: true,
      createdAtUtc: '2026-10-22T12:00:00Z'
    };

    let result: CustomerResponse | null = null;
    service.create(createReq).subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/customers');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(createReq);
    req.flush(createdResponse);

    expect(result).toEqual(createdResponse);
  });

  it('should update customer via PUT /api/customers/{id}', () => {
    const updateReq: UpdateCustomerRequest = {
      name: 'Corporación Alfa Actualizada',
      identificationNumber: 'J-12345678-0',
      email: 'nuevo@alfa.com',
      phone: '+506 2222-3333',
      address: 'Alajuela'
    };

    const updatedResponse: CustomerResponse = {
      ...mockCustomers[0],
      name: updateReq.name,
      email: updateReq.email ?? null,
      address: updateReq.address ?? null
    };

    let result: CustomerResponse | undefined;
    service.update('11111111-1111-1111-1111-111111111111', updateReq).subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/customers/11111111-1111-1111-1111-111111111111');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(updateReq);
    req.flush(updatedResponse);

    expect(result?.name).toBe('Corporación Alfa Actualizada');
  });

  it('should activate customer via POST /api/customers/{id}/activate', () => {
    const activatedResponse: CustomerResponse = {
      ...mockCustomers[1],
      isActive: true
    };

    let result: CustomerResponse | undefined;
    service.activate('22222222-2222-2222-2222-222222222222').subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/customers/22222222-2222-2222-2222-222222222222/activate');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush(activatedResponse);

    expect(result?.isActive).toBe(true);
  });

  it('should deactivate customer via POST /api/customers/{id}/deactivate', () => {
    const deactivatedResponse: CustomerResponse = {
      ...mockCustomers[0],
      isActive: false
    };

    let result: CustomerResponse | undefined;
    service.deactivate('11111111-1111-1111-1111-111111111111').subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/customers/11111111-1111-1111-1111-111111111111/deactivate');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush(deactivatedResponse);

    expect(result?.isActive).toBe(false);
  });
});
