using System;

using ZXMAK2.Engine.Z80;

namespace z80gdbserver
{
	public interface IDebugBridge
	{
		void Initialize(IEmulator emulator);
	}
}

