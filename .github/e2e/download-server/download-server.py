# this script is used to serve the artifacts server locally for
# downloading the artifacts
import http.server
import socketserver

PORT = 8000
DIRECTORY = "./bin/artifacts/"

class SimpleHTTPRequestHandler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=DIRECTORY, **kwargs)

    def do_GET(self):
        # You can add additional logic here if needed
        super().do_GET()

if __name__ == "__main__":
    with socketserver.TCPServer(("", PORT), SimpleHTTPRequestHandler) as httpd:
        print(f"Serving HTTP on port {PORT}...")
        httpd.serve_forever()
