# SistemaInventarioYVentas

Sistema web para la gestión de inventario y ventas de una empresa comercial.

> 🚧 **Proyecto en desarrollo**
>
> El sistema se está construyendo progresivamente utilizando Clean Architecture,
> desarrollo asistido por IA, pruebas automatizadas y prácticas orientadas a
> aplicaciones empresariales.

---

## 🎯 Objetivo

Construir un sistema de inventario y ventas que permita administrar:

- Categorías
- Productos
- Proveedores
- Clientes
- Inventario
- Movimientos de inventario
- Ventas
- Usuarios
- Autenticación
- Autorización
- Auditoría
- Reportes
- Frontend web
- Contenedores Docker
- CI/CD

Uno de los principales objetivos técnicos es garantizar la **consistencia de los datos**, especialmente en las operaciones que afectan simultáneamente las ventas y el inventario.

### Regla de negocio principal

> Una venta nunca debe provocar que el inventario de un producto quede en una cantidad negativa.

---

# 🏗️ Arquitectura

El backend utiliza **Clean Architecture**:

```text
┌─────────────────────────────┐
│            API              │
├─────────────────────────────┤
│       Infrastructure        │
├─────────────────────────────┤
│        Application          │
├─────────────────────────────┤
│           Domain            │
└─────────────────────────────┘