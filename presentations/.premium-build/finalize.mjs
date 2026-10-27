import path from 'node:path';
import {pathToFileURL} from 'node:url';
const root=process.cwd(),build=path.join(root,'presentations/.premium-build');
const skill='C:/Users/OnLine/.codex/plugins/cache/openai-primary-runtime/presentations/26.1015.12040/skills/presentations';
const {finalizePresentation}=await import(pathToFileURL(path.join(skill,'container_tools/artifact_tool_utils.mjs')).href);
const result=await finalizePresentation({workspaceDir:root,candidatePath:path.join(build,'sales-candidate.pptx'),finalPath:path.join(build,'validated/verified-final-v3.pptx'),pythonExecutable:'C:/Users/OnLine/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe',integrityValidatorPath:path.join(skill,'container_tools/inspect_presentation_package_integrity.py'),layoutValidatorPath:path.join(skill,'container_tools/inspect_presentation_layout_geometry.py'),layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-heading-fit'],fontPolicy:{basis:'design',families:['Aptos']},verifyArtifactToolImport:true,receiptPath:path.join(build,'validation-v3.json')});
console.log(JSON.stringify({finalPath:result.finalPath,layout:result.presentationLayout?.finding_count,integrity:result.packageIntegrity?.status}));
