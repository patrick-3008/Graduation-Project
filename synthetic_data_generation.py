import os
import whisper

model = whisper.load_model("turbo")

# Path to your audio folder
audio_folder = "Audio"

# Path to save transcriptions
output_folder = "Synthetic_Transcription"
os.makedirs(output_folder, exist_ok=True)

# Loop through all files in the folder
for filename in os.listdir(audio_folder):
    if filename.endswith((".mp3", ".wav", ".m4a", ".ogg")):
        audio_path = os.path.join(audio_folder, filename)
        final_file_name = filename - '.mp3'
        print(f"Processing: {filename}")
        
        # Open the audio file
        with open(audio_path, "rb") as audio_file:
            response = model.transcribe(audio_file)
        
        # Save transcription to a text file
        output_path = os.path.join(output_folder, f"{final_file_name}.txt")
        with open(output_path, "w", encoding="utf-8") as output_file:
            output_file.write(response["text"])
        
        print(f"Saved transcription: {output_path}")
print("Batch processing complete!")
