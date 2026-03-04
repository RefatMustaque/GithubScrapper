# GitHub Actions SSH Key Setup Guide

This guide explains how to securely set up SSH key authentication for GitHub Actions to deploy to your Linux server. Follow these steps for a secure, automated deployment process.

---

## 1. Generate SSH Key Pair
On your local machine (or wherever you want to generate the key):

```sh
ssh-keygen -t ed25519 -C "github-actions-deploy"
```
- When prompted, enter a filename (e.g., `github_actions_ed25519`) and an optional passphrase.
- This creates two files in `~/.ssh/`:
  - `github_actions_ed25519` (private key)
  - `github_actions_ed25519.pub` (public key)

---

## 2. Copy Public Key to Server
If the `.ssh` directory does not exist on your server, create it:

```sh
ssh username@your_server "mkdir -p ~/.ssh && chmod 700 ~/.ssh"
```

Append your public key to the server's `authorized_keys`:

```sh
cat ~/.ssh/github_actions_ed25519.pub | ssh username@your_server "cat >> ~/.ssh/authorized_keys && chmod 600 ~/.ssh/authorized_keys"
```
- Replace `username` and `your_server` with your actual server username and address.
- For root user, use `/root/.ssh/authorized_keys`.

---

## 3. Troubleshooting
- If you see `No such file or directory`, ensure the `.ssh` directory exists on the server.
- To create both the directory and file with correct permissions:

```sh
ssh username@your_server "mkdir -p ~/.ssh && touch ~/.ssh/authorized_keys && chmod 700 ~/.ssh && chmod 600 ~/.ssh/authorized_keys"
```

---

## 4. Add Private Key to GitHub Secrets
- Open your private key file (e.g., `~/.ssh/github_actions_ed25519`) in a text editor.
- Copy the entire contents.
- Go to your GitHub repository > Settings > Secrets and variables > Actions > New repository secret.
- Name it (e.g., `SSH_PRIVATE_KEY`) and paste the private key.

---

## 5. Why Use `~/.ssh/authorized_keys`?
- SSH only checks for authorized keys in this file inside the user’s home directory.
- This location is secure and protected by file permissions.
- Placing the file elsewhere will not work.

---

## 6. Example: Full Command Sequence
```sh
# Generate key
ssh-keygen -t ed25519 -C "github-actions-deploy"

# Create .ssh directory and set permissions (if needed)
ssh username@your_server "mkdir -p ~/.ssh && chmod 700 ~/.ssh"

# Copy public key to server
cat ~/.ssh/github_actions_ed25519.pub | ssh username@your_server "cat >> ~/.ssh/authorized_keys && chmod 600 ~/.ssh/authorized_keys"

# Add private key to GitHub Secrets
cat ~/.ssh/github_actions_ed25519
```

---

## 7. Security Notes
- Never share your private key. Only add the public key to the server.
- Set correct permissions: `700` for `.ssh` directory, `600` for `authorized_keys`.
- Remove old or unused keys from `authorized_keys` regularly.

---

## 8. References
- [GitHub Actions: Deploy to Linux using SSH](https://docs.github.com/en/actions/deployment/deploying-to-your-cloud-provider/deploying-to-linux-with-ssh)
- [OpenSSH Manual](https://man.openbsd.org/ssh)

---

This guide covers the full process for secure, automated deployments using GitHub Actions and SSH keys.
