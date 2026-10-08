#!/usr/bin/env bash
# Regenerates every Data Heist asset into the Unity repo + the web build.
set -euo pipefail
TOOLS="$(cd "$(dirname "$0")" && pwd)"
REPO="$TOOLS/../.."
RES=$REPO/Assets/DataHeist/Resources/DataHeist
WEB="$REPO/Tools/WebPrototype"
mkdir -p "$RES"/{Characters,Levels,Fonts,Audio/SFX,Audio/Music}

python3 $TOOLS/gen_characters.py "$RES/Characters" /tmp/dh_prev >/dev/null
python3 $TOOLS/gen_env.py "$RES"
python3 $TOOLS/gen_level.py "$RES/Levels/level01.json"
python3 $TOOLS/gen_audio.py /tmp/dh_audio >/dev/null
cp /tmp/dh_audio/SFX/*.wav "$RES/Audio/SFX/"
for f in /tmp/dh_audio/Music/*.wav; do
  ffmpeg -hide_banner -loglevel error -y -i "$f" -c:a libvorbis -q:a 5 "$RES/Audio/Music/$(basename "${f%.wav}").ogg"
done

# web copies
mkdir -p $WEB/build/audio
for f in /tmp/dh_audio/Music/*.wav; do
  ffmpeg -hide_banner -loglevel error -y -i "$f" -c:a libmp3lame -b:a 112k "$WEB/build/audio/$(basename "${f%.wav}").mp3"
done
for f in /tmp/dh_audio/SFX/*.wav; do
  ffmpeg -hide_banner -loglevel error -y -i "$f" -ac 1 -c:a libmp3lame -b:a 64k "$WEB/build/audio/$(basename "${f%.wav}").mp3"
done
python3 $TOOLS/pack_web.py "$RES" "$WEB/build"
echo "assets rebuilt"
