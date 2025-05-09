# Use a stable Ubuntu LTS base image
FROM ubuntu:22.04

# Avoid prompts during package installation
ARG DEBIAN_FRONTEND=noninteractive

# --- Versions and URLs for Tools (Update as needed) ---
# <<< KEEPING .NET SDK 7.0 >>>
ARG DOTNET_SDK_VERSION=7.0
ARG ANDROID_SDK_URL="https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip"
ARG ANDROID_BUILD_TOOLS_VERSION=34.0.0
# <<< CHANGE HERE: Use ilspycmd version compatible with .NET 7 SDK (targets .NET 6) >>>
ARG ILSPYCMD_VERSION=7.2.1.6856

# --- Environment Variables ---
ENV ANDROID_HOME=/opt/android-sdk
ENV ANDROID_SDK_ROOT=${ANDROID_HOME}
ENV JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64
# Add .NET tools path
ENV PATH=${PATH}:${ANDROID_HOME}/cmdline-tools/latest/bin:${ANDROID_HOME}/platform-tools:${ANDROID_HOME}/emulator:${ANDROID_HOME}/build-tools/${ANDROID_BUILD_TOOLS_VERSION}:/root/.dotnet/tools

# --- Install Dependencies (Java, Android Libs, .NET Prerequisites) ---
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        # Java for Android SDK Manager & apktool
        openjdk-17-jdk \
        # Tools for downloading/extracting
        wget \
        unzip \
        zip \
        curl \
        gnupg \
        # Common libraries that might be needed by Android SDK tools
        libc6 \
        libstdc++6 \
        libncurses5 \
        libsdl1.2-dev \
        # Prerequisites for adding Microsoft package repository and installing .NET
        ca-certificates \
        apt-transport-https \
        # <<< KEEPING .NET SDK 7.0 >>>
        dotnet-sdk-${DOTNET_SDK_VERSION} \
        dotnet-runtime-6.0 \
        # Install apktool via apt
        apktool \
        mono-devel \
        # <<< ADDED: Python pip for cloudscraper >>>
        python3-pip \
    # Clean up apt cache to reduce image size
    && rm -rf /var/lib/apt/lists/*

# --- Verify .NET Installation (Optional - helps during build) ---
RUN dotnet --info

# --- Install ilspycmd (.NET Decompiler CLI) ---
# Clear cache just in case (optional but can help)
RUN dotnet nuget locals all --clear
# Install the specified older version - should work with .NET 7 SDK
RUN dotnet tool install ilspycmd -g --version ${ILSPYCMD_VERSION}

# --- Download and Install Android SDK Command-Line Tools ---
RUN mkdir -p ${ANDROID_HOME}/cmdline-tools && \
    wget -q ${ANDROID_SDK_URL} -O /tmp/cmdtools.zip && \
    unzip -q /tmp/cmdtools.zip -d ${ANDROID_HOME}/cmdline-tools && \
    rm /tmp/cmdtools.zip && \
    mkdir -p ${ANDROID_HOME}/cmdline-tools/latest && \
    mv ${ANDROID_HOME}/cmdline-tools/cmdline-tools/* ${ANDROID_HOME}/cmdline-tools/latest/ && \
    rm -rf ${ANDROID_HOME}/cmdline-tools/cmdline-tools

# --- Configure Android SDK ---
RUN yes | sdkmanager --licenses > /dev/null || true

# Install essential SDK components: platform-tools (adb), emulator, and build-tools (contains apksigner)
RUN sdkmanager --install \
    "platform-tools" \
    "build-tools;${ANDROID_BUILD_TOOLS_VERSION}"

# --- Generate Standard Debug Keystore ---
# Create the .android directory in the root user's home
RUN mkdir -p /root/.android && \
    # Generate the debug keystore with standard alias/passwords
    keytool -genkey -v \
            -keystore /root/.android/debug.keystore \
            -storepass android \
            -alias androiddebugkey \
            -keypass android \
            -keyalg RSA \
            -keysize 2048 \
            -validity 10000 \
            -dname "CN=Android Debug,O=Android,C=US"

RUN pip3 install --no-cache-dir cloudscraper

RUN echo "alias init='tools/init.sh'" >> /root/.bashrc
RUN echo "alias modify='tools/modify.sh'" >> /root/.bashrc
RUN echo "alias mod='tools/modify.sh'" >> /root/.bashrc

# --- Final Setup ---
WORKDIR /workspace
CMD ["bash"]