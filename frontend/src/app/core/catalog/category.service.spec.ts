import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CategoryService } from './category.service';
import { CategoryResponse, CreateCategoryRequest, UpdateCategoryRequest } from './catalog.models';

describe('CategoryService', () => {
  let service: CategoryService;
  let httpTesting: HttpTestingController;

  const mockCategories: CategoryResponse[] = [
    {
      id: '11111111-1111-1111-1111-111111111111',
      name: 'Bebidas',
      isActive: true,
      products: [{ identifier: 'BEB-01', name: 'Agua Mineral', isActive: true }]
    },
    {
      id: '22222222-2222-2222-2222-222222222222',
      name: 'Snacks',
      isActive: false,
      products: []
    }
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CategoryService, provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(CategoryService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should retrieve all categories via GET /api/categories', () => {
    let result: CategoryResponse[] = [];
    service.list().subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/categories');
    expect(req.request.method).toBe('GET');
    req.flush(mockCategories);

    expect(result.length).toBe(2);
    expect(result[0].name).toBe('Bebidas');
  });

  it('should retrieve single category via GET /api/categories/{id}', () => {
    let result: CategoryResponse | null = null;
    service.getById('11111111-1111-1111-1111-111111111111').subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/categories/11111111-1111-1111-1111-111111111111');
    expect(req.request.method).toBe('GET');
    req.flush(mockCategories[0]);

    expect(result).toEqual(mockCategories[0]);
  });

  it('should create category via POST /api/categories', () => {
    const newCategoryRequest: CreateCategoryRequest = { name: 'Lácteos' };
    const createdResponse: CategoryResponse = {
      id: '33333333-3333-3333-3333-333333333333',
      name: 'Lácteos',
      isActive: true,
      products: []
    };

    let result: CategoryResponse | null = null;
    service.create(newCategoryRequest).subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/categories');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(newCategoryRequest);
    req.flush(createdResponse);

    expect(result).toEqual(createdResponse);
  });

  it('should update category via PUT /api/categories/{id}', () => {
    const updateRequest: UpdateCategoryRequest = { name: 'Bebidas Frías', isActive: true };
    const updatedResponse: CategoryResponse = {
      id: '11111111-1111-1111-1111-111111111111',
      name: 'Bebidas Frías',
      isActive: true,
      products: []
    };

    let result: CategoryResponse | null = null;
    service.update('11111111-1111-1111-1111-111111111111', updateRequest).subscribe((data) => {
      result = data;
    });

    const req = httpTesting.expectOne('/api/categories/11111111-1111-1111-1111-111111111111');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(updateRequest);
    req.flush(updatedResponse);

    expect(result).toEqual(updatedResponse);
  });

  it('should delete category via DELETE /api/categories/{id}', () => {
    let completed = false;
    service.delete('22222222-2222-2222-2222-222222222222').subscribe(() => {
      completed = true;
    });

    const req = httpTesting.expectOne('/api/categories/22222222-2222-2222-2222-222222222222');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);

    expect(completed).toBe(true);
  });
});
