#pragma once
#include <Windows.h>
#include <windows.devices.h>
#include <Audioclient.h>
#include <mmdeviceapi.h>
#include <ppltasks.h>
#define REFTIMES_PER_SEC  10000000
#define REFTIMES_PER_MILLISEC  10000
namespace ScreenSenderComponent
{
	using namespace concurrency;
	using namespace Windows;
	using namespace Platform;
	using namespace Windows::Foundation;
	using namespace Windows::Devices;
	using namespace Windows::Devices::Enumeration;
	using namespace Windows::Media::MediaProperties;
	using namespace Windows::System::Threading;



	public value struct AudioClientBufferDetails
	{
		int64 DataPointer;
		int64 ByteLength;
		int32 NumSamplesToRead;
		int32 ChannelCount;
		int32 BytesPerMonoSample;
		int32 BytesPerSample;
	};

	public delegate void AudioClientBufferReadyHandler(AudioClientBufferDetails details, int32* numFramesRead);

	class AudioClientCallback : IActivateAudioInterfaceCompletionHandler, IUnknown, IAgileObject
	{
	public:
		task_completion_event<void> tce;
		IAudioClient3* Client;
		AudioClientCallback(task_completion_event<void> task_event)
		{
			tce = task_event;
		}
		virtual HRESULT STDMETHODCALLTYPE ActivateCompleted(IActivateAudioInterfaceAsyncOperation* operation)
		{
			HRESULT resultCode = 0;
			IUnknown* activatedInterface;
			auto hr = operation->GetActivateResult(&resultCode, &activatedInterface);
			if (hr == 0 && resultCode == 0)
			{
				activatedInterface->QueryInterface<IAudioClient3>(&Client);
				activatedInterface->Release();
			}
			tce.set();
			return hr;
		}
		virtual ULONG STDMETHODCALLTYPE AddRef()
		{
			refCount++;
			return refCount;
		}
		virtual ULONG STDMETHODCALLTYPE Release()
		{
			refCount--;
			if (refCount <= 0)
			{
				delete this;
				return 0;
			}
			return refCount;
		}
		virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void ** ppvObject)
		{
			if (riid == IID_IUnknown)
			{
				AddRef();
				*ppvObject = static_cast<IUnknown*>(this);
				return 0;
			}
			else if (riid == __uuidof(IActivateAudioInterfaceCompletionHandler))
			{
				AddRef();
				*ppvObject = static_cast<IActivateAudioInterfaceCompletionHandler*>(this);
				return 0;
			}
			else if (riid == IID_IAgileObject)
			{
				AddRef();
				*ppvObject = static_cast<IAgileObject*>(this);
				return 0;
			}
			return E_NOINTERFACE;
		}
	private:
		ULONG refCount = 1;
	};

	public ref class LoopbackAudioCapture sealed
	{
	public:
		property AudioClientBufferReadyHandler^ BufferReadyDelegate;
		property AudioEncodingProperties^ SuggestedEncodingProperties;
		property AudioEncodingProperties^ EncodingProperties
		{
			AudioEncodingProperties^ get()
			{
				return fEncodingProps;
			}
		}
		property bool Started
		{
			bool get()
			{
				return started;
			}
		}

		property int32 SamplesPerQuantum
		{
			int32 get()
			{
				return samplesPerQuantum;
			}
		}

		property String^ Device
		{
			String^ get()
			{
				return device;
			}
		}

		LoopbackAudioCapture(String^ renderDevice)
		{
			device = renderDevice;
		}

		IAsyncAction^ Start()
		{
			if (started)
				throw ref new FailureException(L"Cannot start loopback capture as it has already been started.");
			started = true;
			task_completion_event<void> tce;
			task<void> event_set(tce);
			auto tce2 = tce;
			callback = new AudioClientCallback(tce);
			IActivateAudioInterfaceCompletionHandler* handler;
			auto hr = callback->QueryInterface(__uuidof(IActivateAudioInterfaceCompletionHandler), (void**)&handler);
			if (hr != 0)
				throw Exception::CreateException(hr);
			IActivateAudioInterfaceAsyncOperation* operation;
			auto action = create_async([this, event_set]
				{
					return event_set.then([this]
						{
							client = callback->Client;
							if (client == nullptr)
							{
								started = false;
							}
							else 
							{
								BeginCapture();
							}
							auto refCount = callback->Release();
						});
				});
			hr = ActivateAudioInterfaceAsync(device->Data(), __uuidof(IAudioClient3), nullptr, handler, &operation);
			// An additional release, because ActivateAudioInterfaceAsync adds two references (one is QueryInterface and the other is AddRef), but only releases one. 
			//It probably doesn't release the reference from the QueryInterface for IAgileObject
			handler->Release();
			handler->Release();
			if (hr != 0)
				throw Exception::CreateException(hr);
			return action;
		}

		IAsyncAction^ Stop()
		{
			if (!started)
				throw ref new FailureException(L"Cannot stop loopback capture as it has not yet started.");;
			task_completion_event<void> tce;
			task<void> stopEvent(tce);
			stoppingTce = tce;
			stoppingTceMade = true;
			auto action = create_async([this, stopEvent]
				{
					started = false;
					if (capturing)
					{
						captureIndex++;
						return stopEvent.then([this]
							{
								ULONG refCount = 0;
								if (client != nullptr)
								{
									client->Stop();
									refCount = client->Release();
								}
								if (captureClient != nullptr)
									refCount = captureClient->Release();
							});
					}
					else 
					{
						captureIndex++;
						stoppingTce.set();
						ULONG refCount = 0;
						if (client != nullptr)
						{
							client->Stop();
							refCount = client->Release();
						}
							
						if (captureClient != nullptr)
							refCount = captureClient->Release();
						return stopEvent;
					}
					
				});
			return action;
		}

		void ChangeDevice(String^ renderDevice)
		{
			if (started)
				throw ref new FailureException(L"Cannot change device while audio capture is running. Call Stop() first, change the device, then call Start().");
			device = renderDevice;
		}
	private:
		AudioEncodingProperties^ fEncodingProps;
		task_completion_event<void> stoppingTce;
		bool stoppingTceMade = false;
		int32 samplesPerQuantum = 480;
		bool capturing;
		bool started;
		String^ device;
		AudioClientCallback* callback;
		IAudioClient3* client;
		IAudioCaptureClient* captureClient;
		REFERENCE_TIME requestedBufferDuration = REFTIMES_PER_MILLISEC * 20;
		REFERENCE_TIME actualBufferDuration;
		WAVEFORMATEX waveFormat, actualWaveFormat;
		UINT32 bufferFrameSize;
		HANDLE eventHandle;
		uint32 captureIndex = 0;
		void BeginCapture()
		{
			HRESULT hr;
			WAVEFORMATEX* mixFormat;
			client->GetMixFormat(&mixFormat);
			waveFormat = *mixFormat;
			if (SuggestedEncodingProperties != nullptr)
			{
				if (SuggestedEncodingProperties->Subtype == L"Float")
				{
					waveFormat.wFormatTag = WAVE_FORMAT_IEEE_FLOAT;
				}
				else
				{
					throw ref new FailureException(L"Format subtype must be float");
				}
				if (SuggestedEncodingProperties->Bitrate > 0)
					waveFormat.nAvgBytesPerSec = SuggestedEncodingProperties->Bitrate / 8;
				if (SuggestedEncodingProperties->BitsPerSample > 0)
					waveFormat.wBitsPerSample = SuggestedEncodingProperties->BitsPerSample;
			}
			else
			{
				waveFormat.wFormatTag = WAVE_FORMAT_IEEE_FLOAT;
				waveFormat.wBitsPerSample = 32;
				waveFormat.nAvgBytesPerSec = (waveFormat.wBitsPerSample / 8) * waveFormat.nChannels * waveFormat.nSamplesPerSec;
				waveFormat.cbSize = 0;
				
			}
				
			hr = client->Initialize(AUDCLNT_SHAREMODE_SHARED, AUDCLNT_STREAMFLAGS_LOOPBACK | AUDCLNT_STREAMFLAGS_EVENTCALLBACK, requestedBufferDuration, 0, &waveFormat, nullptr);
			if (hr != S_OK)
				throw Exception::CreateException(hr);

			uint32 period;
			hr = client->GetCurrentSharedModeEnginePeriod(&mixFormat, &period);
			if (hr != S_OK)
				throw Exception::CreateException(hr);
			samplesPerQuantum = (int32)period;

			actualWaveFormat = *mixFormat;
			fEncodingProps = ref new AudioEncodingProperties();
			fEncodingProps->Bitrate = actualWaveFormat.nAvgBytesPerSec * 8;
			fEncodingProps->ChannelCount = actualWaveFormat.nChannels;
			fEncodingProps->BitsPerSample = actualWaveFormat.wBitsPerSample;
			fEncodingProps->SampleRate = actualWaveFormat.nSamplesPerSec;
			switch (actualWaveFormat.wFormatTag)
			{
			case WAVE_FORMAT_IEEE_FLOAT:
				fEncodingProps->Subtype = L"Float";
				break;
			case WAVE_FORMAT_PCM:
				fEncodingProps->Subtype = L"PCM";
				break;
			}

			hr = client->GetBufferSize(&bufferFrameSize);
			if (hr != S_OK)
				throw Exception::CreateException(hr);

			actualBufferDuration = (double)REFTIMES_PER_SEC * bufferFrameSize / mixFormat->nSamplesPerSec;

			hr = client->GetService(__uuidof(IAudioCaptureClient), (void**)&captureClient);
			if (hr != S_OK)
				throw Exception::CreateException(hr);
			
			eventHandle = CreateEvent(NULL, FALSE, FALSE, NULL);
			hr = client->SetEventHandle(eventHandle);
			if (hr != S_OK)
				throw Exception::CreateException(hr);

			hr = client->Start();
			if (hr != S_OK)
				throw Exception::CreateException(hr);
			captureIndex++;
			ThreadPool::RunAsync(ref new WorkItemHandler(this, &LoopbackAudioCapture::CaptureThread));
		}
		void CaptureThread(IAsyncAction^ action)
		{
			capturing = true;
			auto cIndex = captureIndex;
			HRESULT hr;
			UINT32 sampleCount = 0;
			DWORD flags;
			byte* data;
			AudioClientBufferDetails args = * new AudioClientBufferDetails();
			args.ChannelCount = actualWaveFormat.nChannels;
			args.BytesPerMonoSample = actualWaveFormat.wBitsPerSample / 8;
			args.BytesPerSample = args.BytesPerMonoSample * args.ChannelCount;
			while (capturing && started && cIndex == captureIndex)
			{
				WaitForSingleObject(eventHandle, 2000);
				hr = captureClient->GetBuffer(&data, &sampleCount, &flags, NULL, NULL);
				if (hr == AUDCLNT_S_BUFFER_EMPTY || hr == AUDCLNT_E_OUT_OF_ORDER || hr == AUDCLNT_E_BUFFER_OPERATION_PENDING)
				{
					hr = captureClient->ReleaseBuffer(sampleCount);
					data = nullptr;
					continue;
				}
				else if (hr != S_OK)
				{
					captureClient->ReleaseBuffer(sampleCount);
					data = nullptr;
					break;
				}

				if (flags & AUDCLNT_BUFFERFLAGS_SILENT != 0)
				{
					data = nullptr;
				}
				args.DataPointer = (int64)data;
				args.NumSamplesToRead = sampleCount;
				args.ByteLength = args.NumSamplesToRead * args.BytesPerSample;
				int readSamples = sampleCount;
				if (data != nullptr && BufferReadyDelegate != nullptr)
					BufferReadyDelegate(args, &readSamples);
				captureClient->ReleaseBuffer(readSamples);
			}
			if (stoppingTceMade)
			{
				stoppingTceMade = false;
				stoppingTce.set();
			}
			capturing = false;
		}
	};
}

