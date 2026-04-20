import os
import re

root_path = r"D:\fpt\ky 9\doAn\Tokki_BE\Tokki.UnitTest"

word_pattern = r"\b(?:\w+ly|\w+ing|\w+tions?|test|tests|string|array|check|checks|empty|zero|fail|success|properties|null|filtered|mapping|pagination|sorting)\b"

garbage_pattern = re.compile(rf"(?:\s+{word_pattern}){{4,}}", re.IGNORECASE)

total_files = 0

for dirpath, _, filenames in os.walk(root_path):
    if "obj" in dirpath or "bin" in dirpath:
        continue
    for filename in filenames:
        if filename.endswith(".cs"):
            filepath = os.path.join(dirpath, filename)
            with open(filepath, "r", encoding="utf-8-sig") as f:
                content = f.read()
            
            original_content = content
            
            # Find the Description, ExpectedResult, or AppliedConditions values
            # Using conservative regex
            # e.g., Description = "Valid gracefully safely cleanly ..."
            
            # Actually, let's just replace the garbage pattern anywhere it is before a quote.
            def replacer(match):
                # The match includes all the garbage words
                return ""
            
            # Since regex is greedy, matching (garbage)+(?=") will take as much as possible at the end of the string
            # But the garbage_pattern requires \s+ at the start, meaning it will leave the first word if it doesn't match or even if we don't include it.
            
            new_content = re.sub(rf"({garbage_pattern.pattern})(?=\")", "", content, flags=re.IGNORECASE)
            
            # Also clean up cases where the string ends up with trailing space: `"Valid "` -> `"Valid"`
            new_content = re.sub(r'(\w)\s+\"', r'\1"', new_content)
            # Remove any entirely empty conditions `""` in lists if they happened, but let's just leave string as "" for now.

            if new_content != original_content:
                with open(filepath, "w", encoding="utf-8-sig") as f:
                    f.write(new_content)
                total_files += 1
                print(f"Fixed: {filename}")

print(f"Done! Fixed {total_files} files.")
