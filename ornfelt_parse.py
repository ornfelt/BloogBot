import re
import os
import openai
import time
from enum import Enum

class CodeGenerationOptions(Enum):
    generate_classes = 1
    generate_namespaces = 2
    generate_fields_and_methods = 3

# Settings
option = CodeGenerationOptions.generate_fields_and_methods
openai.api_key = os.getenv("OPENAI_API_KEY")
openai_responses = 3
openai_temperature = 0.6

openai_model = "gpt-3.5-turbo"
#openai_model = "gpt-3.5-turbo-16k"
#openai_model = "gpt-4"
###openai_model = "gpt-4-32k" # 32 tokens

# Performing conditionals on the variable
if option == CodeGenerationOptions.generate_classes:
    print("Generating classes...")
elif option == CodeGenerationOptions.generate_namespaces:
    print("Generating namespaces...")
elif option == CodeGenerationOptions.generate_fields_and_methods:
    print("Generating fields and methods...")

# See this for parameter control (temperature, top_p):
# https://community.openai.com/t/cheat-sheet-mastering-temperature-and-top-p-in-chatgpt-api-a-few-tips-and-tricks-on-controlling-the-creativity-deterministic-output-of-prompt-responses/172683
# Models: https://platform.openai.com/docs/models
def generate_comment(prompt):
    time.sleep(1)
    if option != CodeGenerationOptions.generate_fields_and_methods:
        prompt = shorten_string(prompt, 800)
    try:
        completion = openai.ChatCompletion.create(model=openai_model, messages=[{"role": "user", "content": prompt}], temperature=openai_temperature, top_p=0.2, n=openai_responses)
    except Exception as e:
        if "Please reduce the length of the messages" in str(e):
            print("Code segment contains too many tokens. Trying again with 16k model...")
            completion = openai.ChatCompletion.create(model="gpt-3.5-turbo-16k", messages=[{"role": "user", "content": shorten_string(prompt)}], temperature=openai_temperature, top_p=0.2, n=openai_responses)
        else:
            print("Timeout while generating XML comment. Trying again...")
            completion = openai.ChatCompletion.create(model=openai_model, messages=[{"role": "user", "content": prompt}], temperature=openai_temperature, top_p=0.2, n=openai_responses)
    # Validate generated comment(s)
    for idx, resp in enumerate(completion.choices):
        response = resp.message.content.strip().split("{")[0]
        #print(f"response {idx}: {response}")
        response = '\n'.join([line for line in response.split('\n') if '///' in line])
        if "///" in response and "summary" in response:
            summary_count = response.count("summary>")
            if summary_count == 2:
                return resp.message.content.strip()
    return completion.choices[0].message.content.strip()

def process_file(file_path):
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Split the content into segments
    raw_segments = content.split('--------------ornfelt_parse_separator--------------')
    segments = []

    # Process each raw segment to extract the file path and code
    for raw_segment in raw_segments:
        path_match = re.search(r"ornfelt_parsed_file:\s*(.*)\n", raw_segment)
        if path_match:
            file_path = path_match.group(1).strip()
            code = raw_segment.replace(path_match.group(0), "").strip()
            segments.append((file_path, code))

    return segments
    
# Shorten string to max_length
def shorten_string(input_string, max_length=12000):
    if len(input_string) > max_length:
        # Shorten the string to the maximum length
        shortened_string = input_string[:max_length]
        if max_length == 12000:
            print("Code segment longer than 12k characters, shortening it...")
        return shortened_string
    else:
        return input_string

def main():
    directoryPath = os.getcwd()  # Get current directory

    # Find all .output.txt files
    files = [os.path.join(dp, f) for dp, dn, filenames in os.walk(directoryPath) for f in filenames if f.endswith('.output.txt')]

    for file_path in files:
        segments = process_file(file_path)

        for cs_file_path, segment in segments:
            # Read the associated .cs file
            with open(cs_file_path, 'r', encoding='utf-8') as cs_file:
                cs_content = cs_file.read()

            # Check if the segment is already preceded by a comment
            if segment in cs_content:
                preceding_text = cs_content.split(segment)[0]
            else:
                print("Segment not found in file content?!")
                if shorten_string(segment, 200) in cs_content:
                    preceding_text = cs_content.split(shorten_string(segment, 200))[0]
                    print("Found shortened (200 chars) string...")
                if shorten_string(segment, 50) in cs_content:
                    preceding_text = cs_content.split(shorten_string(segment, 50))[0]
                    print("Found shortened (50 chars) string...")
                else:
                    print(f"Couldn't find segment in file {cs_file_path}. Content: {segment}\nSkipping...")
                    continue
            #last_lines = preceding_text.split('\n')[-4:] # check the last 4 lines before the segment
            last_lines = preceding_text.split('\n')[-2:] # check the last 2 lines before the segment
            # First check for comments in segment
            if option == CodeGenerationOptions.generate_fields_and_methods and "///" in segment:
                print("Comment already exists. Skipping...")
                continue  # if yes, skip to the next segment
            elif option != CodeGenerationOptions.generate_fields_and_methods and "///" in segment.split("{")[0]:
                print("Comment already exists. Skipping...")
                continue  # if yes, skip to the next segment
            elif any("///" in line for line in last_lines):
                print("Comment already exists in last_lines. Skipping...")
                continue  # if yes, skip to the next segment
            
            # Print the content and line count
            print("-------------------------------------------")
            lines = segment.split('\n')
            print(f"Segment in {cs_file_path} with {len(lines)} lines and {len(segment)} characters:")
            # Don't print entire classes / namespaces
            if option == CodeGenerationOptions.generate_fields_and_methods:
                print(segment)
            print("-------------------------------------------\n")

            # Generate comment
            prompt = (
                f"Generate a C# XML comment for the following {'class (ONLY for the class)' if option == CodeGenerationOptions.generate_classes else 'namespace (ONLY for the namespace)'} if it doesn't exist already. I want you to only respond with the provided comment, nothing else. The comments must start with '///' and should always include at least a valid summary tag and the summary should be max 1-2 sentences. For example:\n/// <summary>\n/// This code handles X\n/// </summary>\n Here's my code:\n{segment}."
                if option != CodeGenerationOptions.generate_fields_and_methods
                else f"Generate a C# XML comment for the following code if it doesn't exist already. I want you to only respond with the provided comment, nothing else. The comments must start with '///' and should always include at least a valid summary tag and the summary should be max 1-2 sentences. For example:\n/// <summary>\n/// This code handles X\n/// </summary>\n Here's my code:\n{segment}."
            )
            generated_comment = generate_comment(prompt).split("{")[0]
            original_comment = generated_comment
            generated_comment = '\n'.join([line for line in generated_comment.split('\n') if '///' in line])
            generated_segment = generated_comment + '\n' + segment
            
            # Validate generated comment
            if (not "///" in generated_comment or "summary" not in generated_comment or generated_comment[0] != '/'):
                print(f"ChatGPT didn't include /// or summary for XML comment. Skipping...\n{original_comment}")
                continue
            summary_count = generated_comment.count("summary>")
            if summary_count > 2 or summary_count == 1:
                print(f"ChatGPT generated multiple summary tags / single summary tag. Skipping...\n{original_comment}")
                continue
            
            # Insert the generated_comment
            cs_content = cs_content.replace(segment, generated_segment)
            print("Generated segment (shortened to 500 chars):\n", shorten_string(generated_segment, 500))
            # Write the updated content back to the .cs file
            with open(cs_file_path, 'w', encoding='utf-8') as cs_file:
                cs_file.write(cs_content)

if __name__ == "__main__":
    main()