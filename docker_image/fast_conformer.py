import os
import nemo.collections.asr as nemo_asr
import torch
import librosa
import pandas as pd
from ruamel.yaml import YAML
from omegaconf import OmegaConf
import multiprocessing as mp

def load_asr_model(ckpt_path, device):
    config_path = 'ASR_for_egyptian_dialect/configs/FC-transducer-inference.yaml'
    yaml = YAML(typ='safe')
    with open(config_path) as f:
        params = yaml.load(f)
    params['model'].pop('test_ds', None)
    conf = OmegaConf.create(params)
    
    model = nemo_asr.models.EncDecRNNTBPEModel(cfg=conf['model']).to(device)
    model.load_state_dict(torch.load(ckpt_path, map_location=device, weights_only=False)['state_dict'])
    model.eval()
    return model

def load_audio(audio_path):
    try:
        audio, sr = librosa.load(audio_path, sr=16000)
        if audio is None or len(audio) == 0:
            raise ValueError("Empty or corrupt audio file.")
        return audio, sr
    except Exception as e:
        print(f"Skipping corrupted file {audio_path}: {e}")
        return None, None

def infer(model, audio):
    return model.transcribe([audio])

def process_files(audio_files, model_path, device, model_number, results_list):
    print(f"Loading ASR model {model_number} on {device}...")
    model = load_asr_model(model_path, device)
    
    transcripts = []
    for idx, audio_file in enumerate(audio_files, start=1):
        audio_path = os.path.join("sample_data", audio_file)
        print(f"[{device}] Processing {idx}/{len(audio_files)}: {audio_file} using model {model_number} on {device}...")
        
        audio, sr = load_audio(audio_path)
        if audio is None:
            continue
        
        with torch.no_grad():
            transcript = infer(model, audio)
        
        transcripts.append([audio_file, transcript[0], device, model_number])
    
    results_list.extend(transcripts)

def main():
    audio_dir = "sample_data/"
    output_csv = "t8.csv"
    model_path = "ASR_for_egyptian_dialect/Models/asr_model.ckpt"
    
    audio_files = sorted([f for f in os.listdir(audio_dir) if f.endswith(".mp3")])
    print(f"Total audio files: {len(audio_files)}")
    
    mid_index = len(audio_files) // 2
    audio_files_1 = audio_files[:mid_index]
    audio_files_2 = audio_files[mid_index:]
    
    manager = mp.Manager()
    results_list = manager.list()
    
    device1 = "cuda:0" if torch.cuda.is_available() else "cpu"
    device2 = "cuda:1" if torch.cuda.device_count() > 1 else "cpu"
    
    process1 = mp.Process(target=process_files, args=(audio_files_1, model_path, device1, 1, results_list))
    process2 = mp.Process(target=process_files, args=(audio_files_2, model_path, device2, 2, results_list))
    
    process1.start()
    process2.start()
    
    process1.join()
    process2.join()
    
    df = pd.DataFrame(list(results_list), columns=["Filename", "Transcript", "Device", "Model Number"])
    df.to_csv(output_csv, index=False)
    print("Transcription completed and saved to CSV.")

if __name__ == "__main__":
    mp.set_start_method('spawn')
    main()
