using System.Runtime.InteropServices;
using EvDevSharp.Enums;
using EvDevSharp.EventArgs;
using EvDevSharp.InteropStructs;

namespace EvDevSharp;

public sealed partial class EvDevDevice {
	private CancellationTokenSource? _cts;
	private Task?                    _monitoringTask;

	public void Dispose() {
		this.StopMonitoring();
	}

	/// <summary>
	///     This method starts to read the device's event file on a separate thread and will raise events accordingly.
	/// </summary>
	public void StartMonitoring() {
		if (this._cts is not null && !this._cts.IsCancellationRequested)
			return;

		this._cts            = new CancellationTokenSource();
		this._monitoringTask = Task.Run(Monitor);

		void Monitor() {
			InputEvent inputEvent;
			int        size   = Marshal.SizeOf(typeof(InputEvent));
			byte[]     buffer = new byte[size];

			GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

			using FileStream eventFile = File.OpenRead(this.DevicePath);
			while (!this._cts.Token.IsCancellationRequested) {
				_ = eventFile.Read(buffer, 0, size);
				inputEvent = (InputEvent)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(InputEvent))!;
				switch ((EvDevEventType)inputEvent.type) {
					case EvDevEventType.EvSyn:
						OnSynEvent?.Invoke(this, new OnSynEventArgs((EvDevSynCode)inputEvent.code, inputEvent.value));
						break;

					case EvDevEventType.EvKey:
						OnKeyEvent?.Invoke(this, new OnKeyEventArgs((EvDevKeyCode)inputEvent.code, (EvDevKeyValue)inputEvent.value));
						break;

					case EvDevEventType.EvRel:
						OnRelativeEvent?.Invoke(this, new OnRelativeEventArgs((EvDevRelativeAxisCode)inputEvent.code, inputEvent.value));
						break;

					case EvDevEventType.EvAbs:
						OnAbsoluteEvent?.Invoke(this, new OnAbsoluteEventArgs((EvDevAbsoluteAxisCode)inputEvent.code, inputEvent.value));
						break;

					case EvDevEventType.EvMsc:
						OnMiscellaneousEvent?.Invoke(this, new EvDevEventArgs(inputEvent.code, inputEvent.value));
						break;

					case EvDevEventType.EvSw:
						OnSwitchEvent?.Invoke(this, new EvDevEventArgs(inputEvent.code, inputEvent.value));
						break;

					case EvDevEventType.EvLed:
						OnLedEvent?.Invoke(this, new EvDevEventArgs(inputEvent.code, inputEvent.value));
						break;

					case EvDevEventType.EvSnd:
						OnSoundEvent?.Invoke(this, new EvDevEventArgs(inputEvent.code, inputEvent.value));
						break;

					case EvDevEventType.EvRep:
						OnAutoRepeatEvent?.Invoke(this, new EvDevEventArgs(inputEvent.code, inputEvent.value));
						break;

					case EvDevEventType.EvFf:
						OnForceFeedbackEvent?.Invoke(this, new EvDevEventArgs(inputEvent.code, inputEvent.value));
						break;

					case EvDevEventType.EvPwr:
						OnPowerEvent?.Invoke(this, new EvDevEventArgs(inputEvent.code, inputEvent.value));
						break;

					case EvDevEventType.EvFfStatus:
						OnForceFeedbackStatusEvent?.Invoke(this, new EvDevEventArgs(inputEvent.code, inputEvent.value));
						break;
				}
			}

			handle.Free();
		}
	}

	/// <summary>
	///     This method cancels event file reading for this device.
	/// </summary>
	public void StopMonitoring() {
		this._cts?.Cancel();
		this._monitoringTask?.Wait();
	}

	~EvDevDevice() {
		this.StopMonitoring();
	}
}
