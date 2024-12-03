cd docker_image
docker build -t asr .
docker run -it --name my_container asr /bin/bash -c "cd ASR_for_egyptian_dialect && python fast_conformer.py && exit"
docker cp my_container:workspace/ASR_for_egyptian_dialect/output.txt C:/Users/patrickn/Jupyter_notebooks/Graduation/
