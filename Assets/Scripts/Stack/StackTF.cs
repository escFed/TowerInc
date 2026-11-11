public class StackTF<T>
{
    private sealed class NodoPila
    {
        public T datos;
        public NodoPila siguiente;
    }

    // La estructura siempre guarda la referencia al primer nodo.
    private NodoPila primero;

    public int Cantidad { get; private set; }

    public void InicializarPila()
    {
        primero = null;
        Cantidad = 0;
    }

    public void Apilar(T x)
    {
        // creo un nuevo nodo
        NodoPila nuevo = new NodoPila();
        // le asigno el dato a apilar
        nuevo.datos = x;
        // asigno la referencia al proximo nodo
        nuevo.siguiente = primero;
        // le asigno al nodo "primero" su nuevo valor
        // En una pila, el primero siempre es el ultimo que entro.
        primero = nuevo;
        Cantidad++;
    }
    public void Desapilar()
    {
        // Desapilar es quitar el primer valor de la pila
        // alcanza con asignarle al nodo primero el del siguiente
        primero = primero.siguiente;
        Cantidad--;
    }

    public bool PilaVacia()
    {
        return (primero == null);
    }

    public T Tope()
    {
        // los datos del primer valor estan siempre en el nodo "primero"
        return primero.datos;
    }
}

// Conserva el contrato original de enteros usado por IStackTDA y por el resto
// del proyecto, mientras StackTF<T> permite apilar otros tipos de dato.
public class StackTF : StackTF<int>, IStackTDA
{
}
