import asyncio
import base64

import requests

from UtilCommonsPython.auth import authentification
from UtilCommonsPython.handshake import handshake
from UtilCommonsPython.keys import generate_ed25519_keypair_base64
from pyprotos.Messages import Functions_pb2

eas_address = "http://localhost:8080/"

username = input("Enter username:")
code = input("Enter code:")
node = input("Enter node host:")

prv, pub = generate_ed25519_keypair_base64()
auth = authentification(eas_address, username, code, pub)
print()
print("User certificate:", auth["user_certificate"])
print("EAS certificate:", auth["eas_certificate"])
nt = asyncio.run(handshake(node, username, auth["user_certificate"], prv, False))
print("Node trust certificate:", nt)

print("Getting function panel...")
url = "http://" + node + "/funcs"

response = requests.get(
    url,
    headers={"Authorization": f"Bearer {nt}"}
)
if response.status_code != 200:
    print(response.text)
    raise Exception("Failed to get function panel (code " + str(response.status_code) + ").")

print("Raw response:", response.content[:100])
print("Content-Type:", response.headers.get("Content-Type"))

panel = Functions_pb2.FunctionPanel()
panel.ParseFromString(response.content)
print("Available functions:")
for func in panel.functions:
    print(f"  - {func.name} : {func.description}")
    for param in func.parameters:
        required = "required" if param.is_required else "optional"
        print(f"      * {param.name} ({required})")

# Pick a function to invoke
func_name = input("\nEnter function name to invoke: ")

# Build InvokeRequest
invoke_request = Functions_pb2.InvokeRequest()
invoke_request.caller_id = username

# Fill parameters interactively
target_func = next((f for f in panel.functions if f.name == func_name), None)
if target_func is None:
    raise Exception(f"Function '{func_name}' not found in panel.")

for param in target_func.parameters:
    value = input(f"Enter value for '{param.name}' ({'required' if param.is_required else 'optional'}): ")
    if value:
        invoke_request.parameters[param.name] = value.encode("utf-8")

# Send InvokeRequest
invoke_url = f"http://{node}/funcs/{func_name}"
invoke_response = requests.post(
    invoke_url,
    data=invoke_request.SerializeToString(),
    headers={
        "Authorization": f"Bearer {nt}",
        "Content-Type": "application/x-protobuf"
    }
)

if invoke_response.status_code != 200:
    print(invoke_response.text)
    raise Exception(f"Failed to invoke function '{func_name}' (code {invoke_response.status_code}).")

# Parse InvokeResponse
invoke_result = Functions_pb2.InvokeResponse()
invoke_result.ParseFromString(invoke_response.content)

if invoke_result.success:
    print(f"\nResult: {invoke_result.result.decode('utf-8')}") # TODO Handle other types
else:
    print(f"\nError: {invoke_result.error}")