# ====== STAGE 1: Builder (Install Dependencies) ======
FROM python:3.10-slim AS builder

RUN apt-get update && \
    apt-get install -y sox python3-pip python3-dev git curl && \
    apt-get clean

RUN pip install --upgrade pip
RUN python -m venv /venv

# Set environment path for pip and Python
ENV PATH="/venv/bin:$PATH"

COPY requirements.txt /workspace/requirements.txt
RUN pip install --no-cache-dir -r /workspace/requirements.txt

# ====== STAGE 2: Final Runtime Image ======
FROM python:3.10-slim AS final

RUN apt-get update && \
    apt-get install -y sox git curl && \
    apt-get clean

# Copy the pre-built virtual environment from builder stage
COPY --from=builder /venv /venv

# Set environment path for installed dependencies
ENV PATH="/venv/bin:$PATH"

WORKDIR /workspace/
COPY . /workspace/

CMD [ "bash" ]
