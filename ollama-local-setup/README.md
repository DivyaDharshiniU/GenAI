Ollama Local Setup for LLM Applications

Run Large Language Models **locally** using Ollama and build production-ready AI applications.


## Why Local LLM?

| Cloud APIs | Local LLM (Ollama) |
|-----------|------------------|
| Cost per request | Zero cost |
| Latency | Network dependent | Low latency |
| Privacy | Data leaves system | Fully local |
| Control | Limited | Full control |

---

## Architecture

User → API → Ollama → LLM → Response

---

## Setup Instructions

# 1. Install Ollama (Mac / Linux)
curl -fsSL https://ollama.com/install.sh | sh

# 2. Pull a model (choose based on your RAM)
ollama pull phi3          # 2.7B — works on 8GB RAM
ollama pull llama3.2      # 3B  — works on 8GB RAM
ollama pull mistral       # 7B  — needs 16GB RAM

# 3. Confirm the server is running
curl http://localhost:11434/api/tags