import { Routes } from '@angular/router';
import { adminGuard, authGuard, unauthGuard } from '@core';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('@features/auth/login.component').then((m) => m.LoginComponent),
    title: 'Iniciar Sesión — SIV',
    canActivate: [unauthGuard]
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@core/layout/main-layout.component').then((m) => m.MainLayoutComponent),
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('@features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
        title: 'Dashboard — SIV'
      },
      {
        path: 'categories',
        loadComponent: () =>
          import('@features/categories/categories.component').then((m) => m.CategoriesComponent),
        title: 'Categorías — SIV'
      },
      {
        path: 'products',
        loadComponent: () =>
          import('@features/products/products.component').then((m) => m.ProductsComponent),
        title: 'Productos — SIV'
      },
      {
        path: 'customers',
        loadComponent: () =>
          import('@features/customers/customers.component').then((m) => m.CustomersComponent),
        title: 'Clientes — SIV'
      },
      {
        path: 'suppliers',
        loadComponent: () =>
          import('@features/suppliers/suppliers.component').then((m) => m.SuppliersComponent),
        title: 'Proveedores — SIV'
      },
      {
        path: 'inventory',
        loadComponent: () =>
          import('@features/inventory/inventory.component').then((m) => m.InventoryComponent),
        title: 'Inventario — SIV'
      },
      {
        path: 'sales',
        loadComponent: () =>
          import('@features/sales/sales.component').then((m) => m.SalesComponent),
        title: 'Ventas — SIV'
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('@features/reports/reports.component').then((m) => m.ReportsComponent),
        title: 'Reportes — SIV'
      },
      {
        path: 'audit',
        loadComponent: () =>
          import('@features/audit/audit.component').then((m) => m.AuditComponent),
        title: 'Auditoría — SIV',
        canActivate: [adminGuard]
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
