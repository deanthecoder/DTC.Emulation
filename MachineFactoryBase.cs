// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

namespace DTC.Emulation;

/// <summary>
/// Template for building machine hardware in a single, explicit location.
/// </summary>
public abstract class MachineFactoryBase<TBus, TCpu, TVideo, TAudio, TInput>
{
    public TInput Input { get; private set; }

    public TBus Bus { get; private set; }

    public TCpu Cpu { get; private set; }

    public TVideo Video { get; private set; }

    public TAudio Audio { get; private set; }

    public void Build()
    {
        Input = CreateInput();
        Bus = CreateBus();
        Cpu = CreateCpu(Bus);
        Video = CreateVideo(Bus);
        Audio = CreateAudio(Bus);
    }

    protected abstract TInput CreateInput();

    protected abstract TBus CreateBus();

    protected abstract TCpu CreateCpu(TBus bus);

    protected abstract TVideo CreateVideo(TBus bus);

    protected abstract TAudio CreateAudio(TBus bus);
}
