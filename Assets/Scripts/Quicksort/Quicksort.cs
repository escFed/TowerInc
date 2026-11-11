using System;

public static class Quicksort
{
    // Conserva la API original de enteros y usa la misma partición que la tienda.
    public static int Partition(int[] arr, int left, int right)
    {
        return Partition(arr, left, right, (a, b) => a.CompareTo(b));
    }

    public static void quickSort(int[] arr, int left, int right)
    {
        Sort(arr, left, right, (a, b) => a.CompareTo(b));
    }

    public static void OrdenarProductos(ShopItem[] productos)
    {
        if (productos == null)
            throw new ArgumentNullException(nameof(productos));

        Sort(productos, 0, productos.Length - 1, CompararProductos);
    }

    private static int CompararProductos(ShopItem a, ShopItem b)
    {
        int precio = a.PrecioActual.CompareTo(b.PrecioActual);
        return precio != 0 ? precio : a.Id.CompareTo(b.Id);
    }

    private static void Sort<T>(T[] arr, int left, int right, Comparison<T> comparar)
    {
        if (arr == null)
            throw new ArgumentNullException(nameof(arr));
        if (left >= right)
            return;

        int division = Partition(arr, left, right, comparar);
        Sort(arr, left, division, comparar);
        Sort(arr, division + 1, right, comparar);
    }

    // Partición de Hoare con pivote central. Avanza incluso con claves iguales.
    private static int Partition<T>(T[] arr, int left, int right, Comparison<T> comparar)
    {
        if (arr == null)
            throw new ArgumentNullException(nameof(arr));

        T pivote = arr[left + (right - left) / 2];
        int i = left - 1;
        int j = right + 1;

        while (true)
        {
            do { i++; } while (comparar(arr[i], pivote) < 0);
            do { j--; } while (comparar(arr[j], pivote) > 0);

            if (i >= j)
                return j;

            T temporal = arr[i];
            arr[i] = arr[j];
            arr[j] = temporal;
        }
    }
}
