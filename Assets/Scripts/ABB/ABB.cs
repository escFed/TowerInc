using System;
using System.Collections.Generic;

public class ABB : IABBTDA
{
    public NodoABB raiz;

    public int Raiz()
    {
        return raiz.info;
    }

    public bool ArbolVacio()
    {
        return (raiz == null);
    }

    public void InicializarArbol()
    {
        raiz = null;
    }

    public NodoABB HijoDer()
    {
        return raiz.hijoDer;
    }

    public NodoABB HijoIzq()
    {
        return raiz.hijoIzq;
    }

    public void AgregarElem(ref NodoABB raiz, int x)
    {
        if (raiz == null)
        {
            raiz = new NodoABB();
            raiz.info = x;
        }
        else if (raiz.info > x)
        {
            AgregarElem(ref raiz.hijoIzq, x);
        }
        else if (raiz.info < x)
        {
            AgregarElem(ref raiz.hijoDer, x);
        }
    }

    public bool AgregarProducto(ShopItem producto)
    {
        if (producto == null)
            throw new ArgumentNullException(nameof(producto));

        if (BuscarNodo(producto.Id) != null)
            return false;

        AgregarElem(ref raiz, producto.Id);
        BuscarNodo(producto.Id).producto = producto;
        return true;
    }

    public ShopItem BuscarProducto(int id)
    {
        NodoABB nodo = BuscarNodo(id);
        return nodo == null ? null : nodo.producto;
    }

    public NodoABB BuscarNodo(int id)
    {
        NodoABB actual = raiz;
        while (actual != null)
        {
            if (id == actual.info)
                return actual;

            actual = id < actual.info ? actual.hijoIzq : actual.hijoDer;
        }

        return null;
    }

    public void RecorrerEnOrden(List<ShopItem> productos)
    {
        if (productos == null)
            throw new ArgumentNullException(nameof(productos));

        RecorrerEnOrden(raiz, productos);
    }

    private static void RecorrerEnOrden(NodoABB nodo, List<ShopItem> productos)
    {
        if (nodo == null)
            return;

        RecorrerEnOrden(nodo.hijoIzq, productos);
        if (nodo.producto != null)
            productos.Add(nodo.producto);
        RecorrerEnOrden(nodo.hijoDer, productos);
    }

    public void EliminarElem(ref NodoABB raiz, int x)
    {
        if (raiz != null)
        {
            if (raiz.info == x && (raiz.hijoIzq == null) && (raiz.hijoDer == null))
            {
                raiz = null;
            }
            else if (raiz.info == x && raiz.hijoIzq != null)
            {
                    NodoABB reemplazo = NodoMayor(raiz.hijoIzq);
                    raiz.info = reemplazo.info;
                    raiz.producto = reemplazo.producto;
                EliminarElem(ref raiz.hijoIzq, raiz.info);
            }
            else if (raiz.info == x && raiz.hijoIzq == null)
            {
                    NodoABB reemplazo = NodoMenor(raiz.hijoDer);
                    raiz.info = reemplazo.info;
                    raiz.producto = reemplazo.producto;
                EliminarElem(ref raiz.hijoDer, raiz.info);
            }
            else if (raiz.info < x)
            {
                EliminarElem(ref raiz.hijoDer, x);
            }
            else
            {
                EliminarElem(ref raiz.hijoIzq, x);
            }
        }
    }

    public int mayor(NodoABB a)
    {
        return NodoMayor(a).info;
    }

    public int menor(NodoABB a)
    {
        return NodoMenor(a).info;
    }

    private static NodoABB NodoMayor(NodoABB nodo)
    {
        while (nodo.hijoDer != null)
            nodo = nodo.hijoDer;
        return nodo;
    }

    private static NodoABB NodoMenor(NodoABB nodo)
    {
        while (nodo.hijoIzq != null)
            nodo = nodo.hijoIzq;
        return nodo;
    }
}
