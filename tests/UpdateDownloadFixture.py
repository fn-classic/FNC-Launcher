"""Run HTTPS updater tests with an ephemeral certificate. Requires cryptography."""
from pathlib import Path
from http.server import ThreadingHTTPServer, BaseHTTPRequestHandler
from cryptography import x509
from cryptography.x509.oid import NameOID
from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import rsa
import datetime, hashlib, ipaddress, json, ssl, subprocess, sys, tempfile, threading

payload = b'MZ updater integrity fixture; never executed'
with tempfile.TemporaryDirectory(prefix='ClassicFN-UpdateTLS-') as directory:
    root = Path(directory)
    key = rsa.generate_private_key(public_exponent=65537, key_size=2048)
    name = x509.Name([x509.NameAttribute(NameOID.COMMON_NAME, 'localhost')])
    now = datetime.datetime.now(datetime.timezone.utc)
    certificate = (x509.CertificateBuilder().subject_name(name).issuer_name(name)
                   .public_key(key.public_key()).serial_number(x509.random_serial_number())
                   .not_valid_before(now - datetime.timedelta(minutes=1))
                   .not_valid_after(now + datetime.timedelta(hours=1))
                   .add_extension(x509.SubjectAlternativeName([x509.IPAddress(ipaddress.ip_address('127.0.0.1'))]), False)
                   .sign(key, hashes.SHA256()))
    (root / 'key.pem').write_bytes(key.private_bytes(serialization.Encoding.PEM, serialization.PrivateFormat.PKCS8, serialization.NoEncryption()))
    (root / 'cert.pem').write_bytes(certificate.public_bytes(serialization.Encoding.PEM))
    class Handler(BaseHTTPRequestHandler):
        def log_message(self, *args): pass
        def do_GET(self):
            if self.path == '/manifest':
                version = '2026.10.8.2'
                body = json.dumps(dict(schema=1, version=version, launcherVersion=version,
                                  backendVersion=version, gameserverVersion=version,
                                  installerUrl=base + '/installer', sha256=hashlib.sha256(payload).hexdigest(), size=len(payload))).encode()
            else: body = payload if self.path == '/installer' else b'x' * len(payload)
            self.send_response(200); self.send_header('Content-Length', str(len(body))); self.end_headers()
            self.wfile.write(body[:3] if self.path == '/truncated' else body)
            self.close_connection = True
    server = ThreadingHTTPServer(('127.0.0.1', 0), Handler)
    context = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
    context.load_cert_chain(root / 'cert.pem', root / 'key.pem')
    server.socket = context.wrap_socket(server.socket, server_side=True)
    base = 'https://127.0.0.1:' + str(server.server_port)
    thread = threading.Thread(target=server.serve_forever, daemon=True); thread.start()
    try:
        result = subprocess.run([sys.argv[1], base, certificate.fingerprint(hashes.SHA1()).hex().upper()])
    finally:
        server.shutdown(); server.server_close(); thread.join()
    sys.exit(result.returncode)
