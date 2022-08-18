#pragma once
#include <Windows.h>
#include <Windows.Media.h>
#include <MemoryBuffer.h>
namespace ScreenSenderComponent
{
	using namespace Windows::Foundation;
	using namespace Windows::Media;
	using namespace Windows::Media::Audio;
	using namespace Windows::Media::Render;
	public delegate void InputFrameRequestedHandler(int32 requiredSamples);
	public ref class AudioGraphHelper sealed
	{

		public:
			event InputFrameRequestedHandler^ InputFrameRequested;
			property AudioFrameOutputNode^ FrameOutputNode;
			property AudioFrameInputNode^ FrameInputNode;
			property AudioGraph^ AudioGraph;
			void LockFrameOutputNodeBuffer(int64* dataLocation, uint32* dataLength)
			{
				IMemoryBufferByteAccess* buffer;
				auto frame = FrameOutputNode->GetFrame();
				outputFrameAudioBuffer = frame->LockBuffer(AudioBufferAccessMode::Read);
				*dataLength = outputFrameAudioBuffer->Length;
				outputFrameMemoryBufferReference = outputFrameAudioBuffer->CreateReference();
				reinterpret_cast<IInspectable*>(outputFrameMemoryBufferReference)->QueryInterface<IMemoryBufferByteAccess>(&buffer);
				byte* location;
				uint32 capacity;
				buffer->GetBuffer(&location, &capacity);
				*dataLocation = (int64)location;
				buffer->Release();
			}

			void RegisterInputFrameEvent()
			{
				inputFrameEvent = FrameInputNode->QuantumStarted += ref new Windows::Foundation::TypedEventHandler<Windows::Media::Audio::AudioFrameInputNode ^, Windows::Media::Audio::FrameInputNodeQuantumStartedEventArgs ^>(this, &ScreenSenderComponent::AudioGraphHelper::OnQuantumStarted);
			}
			void UnregisterInputFrameEvent()
			{
				FrameInputNode->QuantumStarted -= inputFrameEvent;
			}
			void CreateInputAudioFrame(int64* dataLocation, uint32 length)
			{
				IMemoryBufferByteAccess* buffer;
				inputAudioFrame = ref new AudioFrame(length);
				inputFrameAudioBuffer = inputAudioFrame->LockBuffer(AudioBufferAccessMode::Read);
				inputFrameMemoryBufferReference = inputFrameAudioBuffer->CreateReference();
				reinterpret_cast<IInspectable*>(inputFrameMemoryBufferReference)->QueryInterface<IMemoryBufferByteAccess>(&buffer);
				byte* location;
				uint32 capacity;
				buffer->GetBuffer(&location, &capacity);
				*dataLocation = (int64)location;
				buffer->Release();
			}

			void EndInputAudioFrame()
			{
				delete inputFrameAudioBuffer;
				delete inputFrameMemoryBufferReference;
				FrameInputNode->AddFrame(inputAudioFrame);
			}

			void UnlockFrameOutputNodeBuffer()
			{
				delete outputFrameAudioBuffer;
				delete outputFrameMemoryBufferReference;
			}
		private:
			Windows::Foundation::EventRegistrationToken inputFrameEvent;
			AudioFrame^ inputAudioFrame;
			AudioBuffer^ inputFrameAudioBuffer; 
			IMemoryBufferReference^ inputFrameMemoryBufferReference;
			AudioBuffer^ outputFrameAudioBuffer;
			IMemoryBufferReference^ outputFrameMemoryBufferReference;
			void OnQuantumStarted(Windows::Media::Audio::AudioFrameInputNode ^sender, Windows::Media::Audio::FrameInputNodeQuantumStartedEventArgs ^args);
	};
}







void ScreenSenderComponent::AudioGraphHelper::OnQuantumStarted(Windows::Media::Audio::AudioFrameInputNode ^sender, Windows::Media::Audio::FrameInputNodeQuantumStartedEventArgs ^args)
{
	InputFrameRequested(args->RequiredSamples);
}
