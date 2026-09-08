namespace PowerMath.Diagnostics
{
    public interface ILogSink
    {
        void Emit(in LogMessage message);
    }
}
