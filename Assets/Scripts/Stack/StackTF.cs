public class StackTF : IStackTDA
{
    // la estructura siempre guarda la referencia a UN nodo
    // en el caso de la pila es al primero
    Nodo primero;

    public void InicializarPila()
    {
        primero = null;
    }

    public void Apilar(int x)
    {
        // creo un nuevo nodo
        Nodo nuevo = new Nodo();
        // le asigno el dato a apilar
        nuevo.datos = x;
        // asigno la referencia al proximo nodo
        nuevo.siguiente = primero;
        // le asigno al nodo "primero" su nuevo valor
        // en el caso de la pila, el primero siempre es el ultimo que entró
        primero = nuevo;
    }
    public void Desapilar()
    {
        // Desapilar es quitar el primer valor de la pila
        // alcanza con asignarle al nodo primero el del siguiente
        primero = primero.siguiente;
    }

    public bool PilaVacia()
    {
        return (primero == null);
    }

    public int Tope()
    {
        // los datos del primer valor estan siempre en el nodo "primero"
        return primero.datos;
    }
}
