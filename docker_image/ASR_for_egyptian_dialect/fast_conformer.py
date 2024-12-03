print('Program Started...')

import nemo.collections.asr as nemo_asr
import torch
import librosa
from ruamel.yaml import YAML
from omegaconf import OmegaConf

# print(f"Nemo version: {nemo.__version__}")
# print(f"PyTorch version: {torch.__version__}")

device = torch.device("cpu")

def load_asr_model(ckpt_path):
    config_path = 'configs/FC-transducer-inference.yaml'
    yaml = YAML(typ='safe')
    with open(config_path) as f:
        params = yaml.load(f)
    params['model'].pop('test_ds', None)
    conf = OmegaConf.create(params)

    model = nemo_asr.models.EncDecRNNTBPEModel(cfg=conf['model']).to(device)
    model.load_state_dict(torch.load(ckpt_path, map_location=device)['state_dict'])
    model.eval()
    return model

def load_audio(audio_path):
    audio, sr = librosa.load(audio_path, sr=16000)  # Ensure correct sample rate
    return audio, sr

def infer(model, audio):
    return model.transcribe([audio])

audio_path = "test_audio_2.wav"

print("Loading ASR model...")
asr_model = load_asr_model("Models/asr_model.ckpt")

print(f"Loading audio from {audio_path}...")
audio, sr = load_audio(audio_path)

print("Starting inference...")
with torch.no_grad():
    transcript = infer(model=asr_model, audio=audio)

print(f"Transcript: {str(transcript[0])}")

with open('output.txt', 'w') as f:
    f.write(f"Reversed Transcript: {transcript[0]}\n")

print('Program Ended...')