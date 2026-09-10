public class Nodo
{
    // datos a almacenar 
    // en este caso un entero
    public int datos;

    // referencia al siguiente Nodo
    public Nodo siguiente;
}

public class QueueTF : IQueueTDA
{
    // primer elemento de la cola
    Nodo primero;
    // ultimo elemento agregado
    Nodo ultimo;

    public void InicializarCola()
    {
        primero = null;
        ultimo = null;
    }

    public void Acolar(int x)
    {
        // creo el nuevo nodo a agregar
        Nodo nuevo = new Nodo();
        nuevo.datos = x;
        nuevo.siguiente = null;

        //Si la cola no esta vacıa
        if (ultimo != null)
        {
            // al nodo "ultimo" le asigno como siguiente el nodo "nuevo"
            ultimo.siguiente = nuevo;
        }
        // el "ultimo" debe referenciar al "nuevo" que entro
        ultimo = nuevo;

        // Si la cola estaba vacıa
        if (primero == null)
        {
            // si hay un solo nodo, "primero" y "ultimo" hacen referencia al mismo nodo
            primero = ultimo;
        }
    }

    public void Desacolar()
    {
        // quitar el primer valor es hacer que el primero sea el siguiente
        primero = primero.siguiente;

        // Si la cola queda vacıa (si primero.siguiente era null)
        if (primero == null)
        {
            ultimo = null;
        }
    }

    public bool ColaVacia()
    {
        return (ultimo == null);
    }

    public int Primero()
    {
        //devuelvo los datos del primer valor
        return primero.datos;
    }
}
