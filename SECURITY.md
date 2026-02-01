# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.5.x   | :white_check_mark: |
| < 1.5   | :x:                |

## Reporting a Vulnerability

If you discover a security vulnerability in Badgie.Migrator, please report it responsibly:

1. **Do not** open a public GitHub issue for security vulnerabilities
2. Email the maintainers directly or use GitHub's private vulnerability reporting
3. Provide a clear description of the vulnerability
4. Include steps to reproduce if possible
5. Allow reasonable time for a fix before public disclosure

## Security Best Practices

When using Badgie.Migrator:

- Keep your connection strings secure and never commit them to source control
- Use environment variables or secure secret management for credentials
- Run migrations with least-privilege database accounts when possible
- Review migration files before executing them in production
