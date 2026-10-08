import asyncio, json, sys
from pathlib import Path
from fastmcp import Client

async def main():
    async with Client('http://127.0.0.1:8080/mcp', timeout=90) as client:
        request = json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'))
        if 'code_file' in request:
            request = {'tool':'execute_code', 'args':{'action':'execute', 'code':Path(request['code_file']).read_text(encoding='utf-8-sig')}}
        if 'resource' in request:
            result = await client.read_resource(request['resource'])
            print(result)
        else:
            result = await client.call_tool(request['tool'], request.get('args',{}))
            print(json.dumps(result.structured_content,ensure_ascii=False) if result.structured_content else result)

asyncio.run(main())
