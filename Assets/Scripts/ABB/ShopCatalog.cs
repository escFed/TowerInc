using System;
using System.Collections.Generic;

public sealed class ShopCatalog
{
    private readonly ABB productosPorId = new ABB();

    public ShopCatalog(IEnumerable<ShopItem> productos)
    {
        if (productos == null)
            throw new ArgumentNullException(nameof(productos));

        foreach (ShopItem producto in productos)
        {
            if (!productosPorId.AgregarProducto(producto))
                throw new ArgumentException($"ID de producto repetido: {producto.Id}", nameof(productos));
        }

        foreach (ShopItem producto in ObtenerPorId())
        {
            HashSet<int> visited = new HashSet<int>();
            ShopItem current = producto;
            while (current.PrerequisiteId >= 0)
            {
                if (!visited.Add(current.Id))
                    throw new ArgumentException("El catálogo contiene un ciclo de mejoras.", nameof(productos));
                current = Buscar(current.PrerequisiteId);
                if (current == null)
                    throw new ArgumentException($"Requisito inexistente de producto {producto.Id}.", nameof(productos));
            }
        }
    }

    public ShopItem Buscar(int id)
    {
        return productosPorId.BuscarProducto(id);
    }

    public ShopItem[] ObtenerPorPrecio()
    {
        List<ShopItem> visibles = new List<ShopItem>();
        productosPorId.RecorrerEnOrden(visibles);
        ShopItem[] resultado = visibles.ToArray();
        Quicksort.OrdenarProductos(resultado);
        return resultado;
    }

    public ShopItem[] ObtenerPorId()
    {
        List<ShopItem> productos = new List<ShopItem>();
        productosPorId.RecorrerEnOrden(productos);
        return productos.ToArray();
    }

    // Precios iniciales configurables aquí hasta que exista una fuente de datos de diseño.
    public static ShopCatalog CrearEjemplo()
    {
        return new ShopCatalog(new[]
        {
            new ShopItem(20, "Cannon adicional", new[] { 1, 5, 11 }, ShopEffectType.TowerSlot, TowerKind.Cannon),
            new ShopItem(10, "Daño torre común", new[] { 2, 4, 7 }, ShopEffectType.Damage, TowerKind.Cannon, 20),
            new ShopItem(30, "Alcance torre común", new[] { 3, 6, 10 }, ShopEffectType.Range, TowerKind.Cannon, 20),
            new ShopItem(60, "Mejorar torre común", new[] { 4, 9 }, ShopEffectType.TowerLevel, TowerKind.Cannon, 10),
            new ShopItem(40, "Desbloquear MG", new[] { 8 }, ShopEffectType.UnlockTower, TowerKind.MG, 20),
            new ShopItem(50, "Desbloquear ML", new[] { 12 }, ShopEffectType.UnlockTower, TowerKind.Missile, 30),
            new ShopItem(110, "MG adicional", new[] { 5, 9, 14 }, ShopEffectType.TowerSlot, TowerKind.MG, 40),
            new ShopItem(120, "ML adicional", new[] { 7, 12, 18 }, ShopEffectType.TowerSlot, TowerKind.Missile, 50),
            new ShopItem(70, "Mejorar MG", new[] { 6, 12 }, ShopEffectType.TowerLevel, TowerKind.MG, 110),
            new ShopItem(80, "Mejorar ML", new[] { 10, 18 }, ShopEffectType.TowerLevel, TowerKind.Missile, 120),
            new ShopItem(90, "Cadencia MG", new[] { 3, 6, 10 }, ShopEffectType.AttackSpeed, TowerKind.MG, 40),
            new ShopItem(100, "Área de misiles", new[] { 5, 9, 14 }, ShopEffectType.BlastRadius, TowerKind.Missile, 50)
        });
    }
}
