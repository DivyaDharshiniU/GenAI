<<<<<<< HEAD
# Ollama Local Setup for LLM Applications #

Run Large Language Models **locally** using Ollama and build production-ready AI applications.
---

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

### 1. Install Ollama

👉 https://ollama.com

```bash
ollama run llama3# 🚀 Ollama Local Setup for LLM Applications

Run Large Language Models **locally** using Ollama and build production-ready AI applications.

This repository is a **practical reference** for:
- AI Engineers
- AI Architects
- Backend Engineers building GenAI systems

---

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
=======
# GenAI Playbook

Designing and building production-ready Generative AI systems — with a focus on architecture, scalability, and real-world impact.

This repository is not a collection of notebooks.

It is a structured journey of how modern AI systems are actually built — from first principles to production-grade applications.

 I am documenting:
- How to design LLM-powered systems
- How to make architectural trade-offs
- How to move from prototypes → scalable AI products

Every module in this repo answers one question:

“How do we take this concept into a real-world system?”

>>>>>>> claude-001
