FROM python:3.10-slim

# Install required dependencies (Python, pip, etc.)
RUN apt-get update && \
    apt-get install -y sox python3-pip python3-dev git curl && \
    apt-get clean

RUN pip install --upgrade pip

COPY requirements.txt /workspace/requirements.txt

# Download packages from requirements file
RUN pip install -r /workspace/requirements.txt

WORKDIR /workspace/

COPY . /workspace/

CMD [ "bash" ]