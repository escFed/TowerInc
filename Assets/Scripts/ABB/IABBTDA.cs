public interface IABBTDA
{
    int Raiz();
    NodoABB HijoIzq();
    NodoABB HijoDer();
    bool ArbolVacio();
    void InicializarArbol();
    void AgregarElem(ref NodoABB n, int x);
    void EliminarElem(ref NodoABB n, int x);
}
