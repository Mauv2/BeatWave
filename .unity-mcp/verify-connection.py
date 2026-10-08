import asyncio
import json
from fastmcp import Client


async def main():
    async with Client("http://127.0.0.1:8080/mcp") as client:
        tools = await client.list_tools()
        resources = await client.list_resources()
        print(json.dumps({"tools_count": len(tools), "resources": [{"name": r.name, "uri": str(r.uri)} for r in resources]}, ensure_ascii=False))
        for resource in resources:
            if "instances" in str(resource.uri):
                result = await client.read_resource(str(resource.uri))
                print("INSTANCES", result)
                instances = json.loads(result[0].text)
                if not instances.get("instance_count"):
                    raise RuntimeError("Unity Editor is not connected")
        for tool in tools:
            if tool.name == "manage_scene":
                scene = await client.call_tool("manage_scene", {"action": "get_active"})
                print("ACTIVE_SCENE", scene)
                if not scene.structured_content.get("success"):
                    raise RuntimeError("Active scene lookup failed")
        print("PROJECT_INFO", await client.read_resource("mcpforunity://project/info"))


asyncio.run(main())
