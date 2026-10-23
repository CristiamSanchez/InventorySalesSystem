import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ProductService } from './product.service';
import { CreateProductRequest, ProductQueryFilter, ProductResponse, UpdateProductRequest } from './catalog.models';

describe('ProductService', () => {
  let service: ProductService;
  let httpTesting: HttpTestingController;

  const mockProducts: ProductResponse[] = [
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

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ProductService, provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(ProductService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should retrieve all products via GET /api/products without filter parameters', () => {
    let result: ProductResponse[] = [];
    service.list().subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/products');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);
    req.flush(mockProducts);

    expect(result.length).toBe(2);
    expect(result[0].identifier).toBe('BEB-01');
  });

  it('should append categoryId, isActive, and searchTerm query parameters when filtering', () => {
    const filter: ProductQueryFilter = {
      categoryId: '11111111-1111-1111-1111-111111111111',
      isActive: true,
      searchTerm: 'Agua'
    };

    service.list(filter).subscribe();

    const req = httpTesting.expectOne((r) => r.url === '/api/products');
    expect(req.request.params.get('categoryId')).toBe('11111111-1111-1111-1111-111111111111');
    expect(req.request.params.get('isActive')).toBe('true');
    expect(req.request.params.get('searchTerm')).toBe('Agua');
    req.flush([mockProducts[0]]);
  });

  it('should retrieve single product via GET /api/products/{identifier}', () => {
    let result: ProductResponse | null = null;
    service.getByIdentifier('BEB-01').subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/products/BEB-01');
    expect(req.request.method).toBe('GET');
    req.flush(mockProducts[0]);

    expect(result).toEqual(mockProducts[0]);
  });

  it('should create product via POST /api/products', () => {
    const request: CreateProductRequest = {
      identifier: 'BEB-02',
      name: 'Jugo de Naranja',
      categoryId: '11111111-1111-1111-1111-111111111111'
    };
    const createdResponse: ProductResponse = {
      ...request,
      categoryName: 'Bebidas',
      isActive: true
    };

    let result: ProductResponse | null = null;
    service.create(request).subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/products');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(createdResponse);

    expect(result).toEqual(createdResponse);
  });

  it('should update product via PUT /api/products/{identifier}', () => {
    const request: UpdateProductRequest = {
      name: 'Agua Mineral Gas 500ml',
      categoryId: '11111111-1111-1111-1111-111111111111',
      isActive: true
    };
    const updatedResponse: ProductResponse = {
      identifier: 'BEB-01',
      name: 'Agua Mineral Gas 500ml',
      categoryId: '11111111-1111-1111-1111-111111111111',
      categoryName: 'Bebidas',
      isActive: true
    };

    let result: ProductResponse | null = null;
    service.update('BEB-01', request).subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/products/BEB-01');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(updatedResponse);

    expect(result).toEqual(updatedResponse);
  });
});
