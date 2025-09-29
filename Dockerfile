# ===================================================================
# Dockerfile Multi-Stage - Générateur DOE v2.1.3
# ===================================================================
# Image optimisée pour conteneur Linux avec PuppeteerSharp
# ===================================================================

# ===================================================================
# STAGE 1: Build Stage
# ===================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
LABEL stage=build

WORKDIR /source

# Copy project files and restore dependencies
COPY GenerateurDOE.csproj ./
RUN dotnet restore --runtime linux-x64

# Copy source code
COPY . .

# Build and publish application
RUN dotnet publish \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained true \
    --no-restore \
    --output /app/publish \
    --verbosity minimal

# ===================================================================
# STAGE 2: Runtime Stage
# ===================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Install dependencies for PuppeteerSharp (Chromium)
RUN apt-get update && apt-get install -y \
    # Core dependencies
    wget \
    curl \
    gnupg2 \
    ca-certificates \
    apt-transport-https \
    # Chromium dependencies
    libasound2 \
    libatk-bridge2.0-0 \
    libatk1.0-0 \
    libatspi2.0-0 \
    libcups2 \
    libdbus-1-3 \
    libdrm2 \
    libgtk-3-0 \
    libnspr4 \
    libnss3 \
    libwayland-client0 \
    libx11-6 \
    libx11-xcb1 \
    libxcb1 \
    libxcomposite1 \
    libxcursor1 \
    libxdamage1 \
    libxext6 \
    libxfixes3 \
    libxi6 \
    libxkbcommon0 \
    libxrandr2 \
    libxrender1 \
    libxss1 \
    libxtst6 \
    # Additional dependencies
    libappindicator3-1 \
    libasound2-dev \
    libgconf-2-4 \
    libxshmfence1 \
    # ✅ PDFSharp Font Dependencies - CRITIQUE pour génération PDF
    fontconfig \
    fonts-dejavu \
    fonts-liberation \
    fonts-freefont-ttf \
    # Cleanup
    && rm -rf /var/lib/apt/lists/*

# Create application user (security best practice)
RUN useradd -m -s /bin/bash appuser

# Create application directories with proper permissions
WORKDIR /app
RUN mkdir -p \
    /app/Documents/PDF \
    /app/Documents/Images \
    /app/Logs \
    /app/DataProtection-Keys \
    /app/wwwroot/temp \
    && chown -R appuser:appuser /app \
    && chmod -R 755 /app/Documents \
    && chmod -R 777 /app/Logs \
    && chmod -R 755 /app/DataProtection-Keys \
    && chmod -R 755 /app/wwwroot

# Copy application files from build stage
COPY --from=build --chown=appuser:appuser /app/publish .

# Switch to non-root user
USER appuser

# ===================================================================
# STAGE 3: Final Production Image
# ===================================================================
FROM base AS final

# Environment variables for production
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:5000 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    # PuppeteerSharp configuration
    PUPPETEER_SKIP_CHROMIUM_DOWNLOAD=true \
    PUPPETEER_EXECUTABLE_PATH=/usr/bin/google-chrome-stable

# Install Google Chrome (more stable than Chromium for PuppeteerSharp)
USER root
RUN wget -q -O - https://dl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" > /etc/apt/sources.list.d/google-chrome.list \
    && apt-get update \
    && apt-get install -y google-chrome-stable \
    && rm -rf /var/lib/apt/lists/*

# Create all Chrome directories and ensure proper permissions
RUN mkdir -p /tmp/chrome-user \
    && mkdir -p /tmp/chrome-data \
    && mkdir -p /tmp/chrome-cache \
    && mkdir -p /tmp/chrome-crashes \
    && mkdir -p /home/appuser \
    && chmod -R 777 /tmp/chrome-user \
    && chmod -R 777 /tmp/chrome-data \
    && chmod -R 777 /tmp/chrome-cache \
    && chmod -R 777 /tmp/chrome-crashes \
    && chown -R appuser:appuser /home/appuser

# Switch back to application user
USER appuser

# Set environment variables for Chrome stateless mode
ENV HOME=/home/appuser \
    XDG_CONFIG_HOME=/tmp \
    XDG_CACHE_HOME=/tmp \
    XDG_DATA_HOME=/tmp \
    CHROME_USER_DATA_DIR=/tmp/chrome-user \
    CHROME_CACHE_DIR=/tmp/chrome-cache

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Expose port
EXPOSE 5000

# Entry point
ENTRYPOINT ["dotnet", "GenerateurDOE.dll"]