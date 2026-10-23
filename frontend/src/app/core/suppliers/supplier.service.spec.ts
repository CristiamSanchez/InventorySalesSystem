import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SupplierService } from './supplier.service';
import {
  CreateSupplierRequest,
  SupplierResponse,
  UpdateSupplierRequest
} from './supplier.models';

describe('SupplierService', () => {
  let service: SupplierService;
  let httpTesting: HttpTestingController;

  const mockSuppliers: SupplierResponse[] = [
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

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SupplierService, provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(SupplierService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should retrieve all suppliers via GET /api/suppliers without query params', () => {
    let result: SupplierResponse[] = [];
    service.list().subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/suppliers');
    expect(req.request.method).toBe('GET');
    req.flush(mockSuppliers);

    expect(result.length).toBe(2);
    expect(result[0].name).toBe('Proveedor Mayorista Central S.A.');
    expect(result[0].contactName).toBe('Carlos Mendoza');
  });

  it('should retrieve filtered suppliers with query params via GET /api/suppliers', () => {
    let result: SupplierResponse[] = [];
    service.list({ isActive: true, searchTerm: 'Central' }).subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne((r) =>
      r.url === '/api/suppliers' &&
      r.params.get('isActive') === 'true' &&
      r.params.get('searchTerm') === 'Central'
    );
    expect(req.request.method).toBe('GET');
    req.flush([mockSuppliers[0]]);

    expect(result.length).toBe(1);
    expect(result[0].id).toBe(mockSuppliers[0].id);
  });

  it('should retrieve single supplier by ID via GET /api/suppliers/{id}', () => {
    let result: SupplierResponse | null = null;
    service.getById('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa').subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/suppliers/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
    expect(req.request.method).toBe('GET');
    req.flush(mockSuppliers[0]);

    expect(result).toEqual(mockSuppliers[0]);
  });

  it('should create supplier via POST /api/suppliers', () => {
    const createReq: CreateSupplierRequest = {
      name: 'Distribuciones Pacífica',
      contactName: 'Lucía Fernández',
      identificationNumber: 'J-31002233-4',
      email: 'pedidos@pacifica.com',
      phone: '8999-7777',
      address: 'Puntarenas'
    };

    const createdResponse: SupplierResponse = {
      id: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
      name: createReq.name,
      contactName: createReq.contactName ?? null,
      identificationNumber: createReq.identificationNumber ?? null,
      email: createReq.email ?? null,
      phone: createReq.phone ?? null,
      address: createReq.address ?? null,
      isActive: true,
      createdAtUtc: '2026-10-22T13:00:00Z'
    };

    let result: SupplierResponse | null = null;
    service.create(createReq).subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/suppliers');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(createReq);
    req.flush(createdResponse);

    expect(result).toEqual(createdResponse);
  });

  it('should update supplier via PUT /api/suppliers/{id}', () => {
    const updateReq: UpdateSupplierRequest = {
      name: 'Proveedor Mayorista Central Modificado',
      contactName: 'Carlos M. Mendoza',
      identificationNumber: 'J-30112233-1',
      email: 'nuevo@central.com',
      phone: '+506 2555-1111',
      address: 'Cartago Centro'
    };

    const updatedResponse: SupplierResponse = {
      ...mockSuppliers[0],
      name: updateReq.name,
      contactName: updateReq.contactName ?? null,
      email: updateReq.email ?? null,
      address: updateReq.address ?? null
    };

    let result: SupplierResponse | undefined;
    service.update('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', updateReq).subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/suppliers/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(updateReq);
    req.flush(updatedResponse);

    expect(result?.name).toBe('Proveedor Mayorista Central Modificado');
    expect(result?.contactName).toBe('Carlos M. Mendoza');
  });

  it('should activate supplier via POST /api/suppliers/{id}/activate', () => {
    const activatedResponse: SupplierResponse = {
      ...mockSuppliers[1],
      isActive: true
    };

    let result: SupplierResponse | undefined;
    service.activate('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb').subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/suppliers/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/activate');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush(activatedResponse);

    expect(result?.isActive).toBe(true);
  });

  it('should deactivate supplier via POST /api/suppliers/{id}/deactivate', () => {
    const deactivatedResponse: SupplierResponse = {
      ...mockSuppliers[0],
      isActive: false
    };

    let result: SupplierResponse | undefined;
    service.deactivate('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa').subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/suppliers/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/deactivate');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush(deactivatedResponse);

    expect(result?.isActive).toBe(false);
  });
});
