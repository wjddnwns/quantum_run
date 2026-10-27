namespace QuantumCrossy.Input
{
    /// <summary>
    /// 키보드와 스와이프가 같은 MoveCommand를 내게 하는 입력 원천.
    /// </summary>
    public interface IMoveSource
    {
        bool TryPoll(out MoveCommand command);
    }
}
