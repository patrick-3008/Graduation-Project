import os
from youtubesearchpython import VideosSearch
import yt_dlp

def search_and_download(search_query, save_path, max_results=3):
    try:
        # Search for videos
        print(f"Searching for: {search_query}")
        search = VideosSearch(search_query, limit=max_results)
        results = search.result()["result"]

        if not results:
            print("No results found.")
            return

        # Process each video result
        for index, video in enumerate(results):
            video_url = video["link"]
            video_title = video["title"]

            
            print(f"\nDownloading {index + 1}/{max_results}: {video_title}")
            print(f"Video URL: {video_url}")
            if video_title not in os.listdir(save_directory):
                try:
                    # Use yt_dlp to download audio
                    ydl_opts = {
                        'format': 'bestaudio/best',
                        'outtmpl': os.path.join(save_path, f'{video_title}.%(ext)s'),
                        'postprocessors': [{
                            'key': 'FFmpegExtractAudio',
                            'preferredcodec': 'mp3',
                            'preferredquality': '192',
                        }],
                        'quiet': True,
                        'ffmpeg_location': r'C:\ffmpeg-2024-11-18-git-970d57988d-full_build\bin'  
                    }
                    with yt_dlp.YoutubeDL(ydl_opts) as ydl:
                        ydl.download([video_url])
                    print(f"Successfully downloaded: {video_title}")
                except Exception as e:
                    print(f"Skipping {video_title} due to an error: {e}")
                else:
                    print(f"The Audio exsists{video_title}")
    except Exception as e:
        print(f"An error occurred during the search: {e}")

# Example usage
search_query = "مصر"  # Replace with your search term
save_directory = "./Audio"  # Change this to your desired save directory

# Create the directory if it doesn't exist
if not os.path.exists(save_directory):
    os.makedirs(save_directory)

search_and_download(search_query, save_directory, max_results=3)
