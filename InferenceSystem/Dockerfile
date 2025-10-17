FROM ubuntu:18.04

# Install sudo apt-get requirements
RUN apt-get update && apt-get install -y \
        libsm6 \
        libxext6 \
        libxrender-dev \
        libglib2.0-0 \
        ffmpeg \
        python3 \
        python3-pip \
        python3-distutils \
        unzip \
        git \
        curl

# Install pip requirements
WORKDIR /usr/src/install
COPY ./requirements.txt ./requirements.txt
# Upgrade pip to 21.3.1 using get-pip.py to support --no-build-isolation option
# Use --no-build-isolation to avoid orca-hls-utils build system requiring setuptools==70.0.0
# which doesn't exist for Python 3.6. System setuptools (39.0.1) and wheel are sufficient.
RUN curl -sSk https://bootstrap.pypa.io/pip/3.6/get-pip.py | python3 && \
    python3 -m pip install --no-build-isolation -r requirements.txt

# From now on, we will be working in the app's running directory
WORKDIR /usr/src/app

# Put model in place at WORKDIR/model
COPY ./model.zip ./model.zip
RUN unzip ./model.zip
RUN rm ./model.zip

# Move source code to app working directory
COPY ./src/ ./src

CMD ["python3", "-u", "./src/LiveInferenceOrchestrator.py"]
