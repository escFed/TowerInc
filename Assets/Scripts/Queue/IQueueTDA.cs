public interface IQueueTDA
{
    void InicializarCola();

    void Acolar(int x);

    void Desacolar();

    bool ColaVacia();

    int Primero();
}

