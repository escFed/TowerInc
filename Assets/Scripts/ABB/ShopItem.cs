using System;

public sealed class ShopItem
{
    private readonly int[] preciosPorCompra;

    public int Id { get; }
    public string Nombre { get; }
    public ShopEffectType Efecto { get; }
    public TowerKind Torre { get; }
    public int PrerequisiteId { get; }
    public int PrecioBase => preciosPorCompra[0];
    public int NivelMaximo => preciosPorCompra.Length;
    public int NivelActual { get; private set; }
    public bool PuedeMejorarse => NivelActual < NivelMaximo;
    // Los productos agotados quedan al final de la lista ordenada.
    public int PrecioActual => PuedeMejorarse ? preciosPorCompra[NivelActual] : int.MaxValue;

    public ShopItem(int id, string nombre, int[] preciosPorCompra)
        : this(id, nombre, preciosPorCompra, ShopEffectType.None, TowerKind.Cannon)
    {
    }

    public ShopItem(int id, string nombre, int[] preciosPorCompra,
        ShopEffectType efecto, TowerKind torre, int prerequisiteId = -1)
    {
        if (id < 0 || string.IsNullOrWhiteSpace(nombre) || preciosPorCompra == null ||
            preciosPorCompra.Length == 0 || prerequisiteId < -1 || prerequisiteId == id)
        {
            throw new ArgumentException("Los datos del producto de tienda no son válidos.");
        }

        foreach (int precio in preciosPorCompra)
        {
            if (precio < 1)
                throw new ArgumentException("Cada compra debe tener un precio positivo.", nameof(preciosPorCompra));
        }

        Id = id;
        Nombre = nombre;
        Efecto = efecto;
        Torre = torre;
        PrerequisiteId = prerequisiteId;
        this.preciosPorCompra = (int[])preciosPorCompra.Clone();
    }

    public bool SubirNivel()
    {
        if (!PuedeMejorarse)
            return false;

        NivelActual++;
        return true;
    }

}
