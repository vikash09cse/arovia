export type DischargeProcedureKey =
  | ''
  | 'TURP'
  | 'OIU'
  | 'RIRS'
  | 'URSL'
  | 'PCNL'
  | 'DJ'
  | 'Cystoscopy'
  | 'Other';

export interface DischargeMedicine {
  id: string;
  name: string;
  strength: string;
  dose: string;
  route: string;
  frequency: string;
  duration: string;
  timing: string;
}

export interface DischargeForm {
  admissionId: string;
  dateOfSurgery: string;
  dateOfDischarge: string;
  finalDiagnosis: string;
  chiefComplaints: string;
  briefHistory: string;
  investigations: Record<string, string>;
  investigationDates: Record<string, string>;
  procedureDetails: string;
  findings: string;
  conditionAtDischarge: string;
  medications: string;
  advice: string;
  followUp: string;
  fatherName: string;
  pastHistory: {
    comorbidities: string;
    pastSurgery: string;
    allergy: string;
    addiction: string;
  };
  examination: {
    general: string;
    pulse: string;
    bp: string;
    temp: string;
    spo2: string;
    systemic: string;
    perAbdomen: string;
    genitalia: string;
  };
  treatmentDuringAdmission: string;
  anaesthesia: string;
  assistantSurgeon: string;
  indication: string;
  intraopComplications: string;
  conditionExtras: {
    pulse: string;
    bp: string;
    temp: string;
    spo2: string;
    urineOutput: string;
    oralIntake: string;
    ambulation: string;
  };
  followUpDate: string;
  followUpDepartment: string;
  followUpInvestigations: string;
  procedureKey: DischargeProcedureKey;
  procedureExtras: Record<string, string>;
  diagnosisKey: string;
  diagnosisOther: string;
  generalCondition: string;
  generalConditionOther: string;
  postopCourse: string;
  postopCourseDetails: string;
  catheterKey: string;
  catheterOther: string;
  specimen: string;
  dischargeType: string;
  followUpKey: string;
  followUpOther: string;
  followUpNotes: string;
  medicines: DischargeMedicine[];
}

export interface DiagnosisMaster {
  id: string;
  code: string;
  name: string;
  pack: Record<string, unknown>;
}

export interface ProcedureFieldDef {
  key: string;
  label: string;
  kind?: 'text' | 'select' | 'number';
  options?: string[];
  placeholder?: string;
}

export const INVESTIGATION_ROWS: { key: string; label: string }[] = [
  { key: 'cbc', label: 'CBC' },
  { key: 'kft', label: 'KFT' },
  { key: 'urine', label: 'Urine' },
  { key: 'usgCt', label: 'USG/CT' },
  { key: 'lft', label: 'LFT' },
  { key: 'vm', label: 'Viral markers' },
  { key: 'rbs', label: 'RBS' },
  { key: 'bg', label: 'Blood group' },
  { key: 'cxrEcg', label: 'CXR/ECG' }
];

export const CATHETER_OPTIONS = [
  '16F PUC in situ',
  '18F PUC in situ',
  '22F 3-way PUC in situ',
  'Catheter removed',
  'DJ stent in situ',
  'No catheter / drain',
  'Other'
];

export const SPECIMEN_OPTIONS = ['Sent for HPE', 'HPE pending', 'Not applicable'];

export const DISCHARGE_TYPE_OPTIONS = [
  'Routine discharge',
  'Discharge on request',
  'LAMA',
  'Referred to higher centre',
  'Transfer'
];

export const FOLLOW_UP_OPTIONS = [
  'PUC removal',
  'DJ stent removal',
  'Uroflowmetry',
  'HPE review',
  'Routine OPD follow-up',
  'Other'
];

export const MEDICINE_FREQUENCY_OPTIONS = [
  'OD', 'BD', 'TDS', 'QID', 'HS', 'BBF', 'SOS', 'STAT', 'Weekly'
];

export const PROCEDURE_OPTIONS: { key: DischargeProcedureKey; label: string }[] = [
  { key: '', label: 'Select procedure' },
  { key: 'TURP', label: 'TURP' },
  { key: 'OIU', label: 'OIU' },
  { key: 'RIRS', label: 'RIRS' },
  { key: 'URSL', label: 'URSL' },
  { key: 'PCNL', label: 'PCNL' },
  { key: 'DJ', label: 'DJ stenting' },
  { key: 'Cystoscopy', label: 'Cystoscopy' },
  { key: 'Other', label: 'Other' }
];

const PROCEDURE_FIELDS: Record<string, ProcedureFieldDef[]> = {
  TURP: [
    { key: 'bpeFindings', label: 'BPE findings' },
    { key: 'resection', label: 'Resection' },
    { key: 'chips', label: 'Chips' },
    { key: 'catheter', label: 'Catheter' },
    { key: 'irrigation', label: 'Irrigation' }
  ],
  OIU: [
    { key: 'strictureSite', label: 'Stricture site' },
    { key: 'length', label: 'Length' },
    { key: 'knife', label: 'Knife' },
    { key: 'catheter', label: 'Catheter' }
  ],
  RIRS: [
    { key: 'side', label: 'Side', kind: 'select', options: ['Right', 'Left', 'Bilateral'] },
    { key: 'stoneLocation', label: 'Stone location', kind: 'select', options: ['Upper calyx', 'Middle calyx', 'Lower calyx', 'Pelvis'] },
    { key: 'stoneSize', label: 'Stone size (mm)', kind: 'number', placeholder: 'mm' },
    { key: 'laser', label: 'Laser', kind: 'select', options: ['Holmium', 'Thulium', 'Other'] },
    { key: 'fragmentation', label: 'Fragmentation', kind: 'select', options: ['Dusting', 'Fragmentation', 'Both'] },
    { key: 'djStent', label: 'DJ stent', kind: 'select', options: ['Yes', 'No'] },
    { key: 'residual', label: 'Residual fragments', kind: 'select', options: ['None', 'Dust', 'Fragments', 'Significant residual'] }
  ],
  URSL: [
    { key: 'side', label: 'Side', kind: 'select', options: ['Right', 'Left', 'Bilateral'] },
    { key: 'uretericLevel', label: 'Ureteric level' },
    { key: 'stone', label: 'Stone' },
    { key: 'lithotripsy', label: 'Lithotripsy' },
    { key: 'djStent', label: 'DJ stent', kind: 'select', options: ['Yes', 'No'] }
  ],
  PCNL: [
    { key: 'side', label: 'Side', kind: 'select', options: ['Right', 'Left', 'Bilateral'] },
    { key: 'accessCalyx', label: 'Access calyx' },
    { key: 'tractSize', label: 'Tract size' },
    { key: 'stoneClearance', label: 'Stone clearance' },
    { key: 'nephrostomy', label: 'Nephrostomy' }
  ],
  DJ: [
    { key: 'side', label: 'Side', kind: 'select', options: ['Right', 'Left', 'Bilateral'] },
    { key: 'indication', label: 'Indication' },
    { key: 'stentSize', label: 'Stent size' }
  ],
  Cystoscopy: [
    { key: 'findings', label: 'Findings' },
    { key: 'intervention', label: 'Intervention' },
    { key: 'catheter', label: 'Catheter' }
  ]
};

export function procedureFieldDefs(key: DischargeProcedureKey): ProcedureFieldDef[] {
  if (!key || key === 'Other') return [];
  return PROCEDURE_FIELDS[key] ?? [];
}

export function commonMedicinePack(): DischargeMedicine[] {
  return [
    { id: 'med_fastclav', name: 'T. Fastclav', strength: '500 mg', dose: '1 tablet', route: 'Oral', frequency: 'BD', duration: '10 days', timing: 'After food' },
    { id: 'med_ultracet', name: 'T. Ultracet', strength: '', dose: '1 tablet', route: 'Oral', frequency: 'BD', duration: '5 days', timing: 'After food' },
    { id: 'med_sompraz', name: 'T. Sompraz', strength: '40 mg', dose: '1 tablet', route: 'Oral', frequency: 'OD', duration: '5 days', timing: 'BBF' },
    { id: 'med_urotone', name: 'T. Urotone SR', strength: '75 mg', dose: '1 tablet', route: 'Oral', frequency: 'OD', duration: '10 days', timing: '' },
    { id: 'med_urinex', name: 'Syp. Urinex', strength: '', dose: '10 ml', route: 'Oral', frequency: 'TDS', duration: '5 days', timing: '' }
  ];
}

export function emptyMedicine(): DischargeMedicine {
  return {
    id: `med_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
    name: '',
    strength: '',
    dose: '',
    route: 'Oral',
    frequency: '',
    duration: '',
    timing: ''
  };
}

function pickLabel(key: string, other: string): string {
  if (key === 'Other') return other.trim();
  return key.trim();
}

export function composeDischargeForm(form: DischargeForm): DischargeForm {
  const diagnosis = pickLabel(form.diagnosisKey, form.diagnosisOther);
  const key = form.procedureKey;
  const extras = form.procedureExtras ?? {};
  const findingLines: string[] = [];
  for (const def of procedureFieldDefs(key)) {
    const val = (extras[def.key] ?? '').trim();
    if (val) findingLines.push(`${def.label}: ${val}`);
  }
  const catheter = pickLabel(form.catheterKey, form.catheterOther);
  if (catheter) findingLines.push(`Catheter / drain: ${catheter}`);
  if (form.specimen) findingLines.push(`Specimen: ${form.specimen}`);

  let procedureDetails = form.procedureDetails;
  if (key === 'Other') {
    procedureDetails = (extras['otherNote'] ?? '').trim() || 'Other procedure';
  } else if (key) {
    const highlight: string[] = [key];
    for (const k of ['side', 'stoneLocation', 'stoneSize', 'strictureSite', 'resection', 'indication']) {
      const v = (extras[k] ?? '').trim();
      if (v) highlight.push(k === 'stoneSize' ? `${v} mm` : v);
    }
    procedureDetails = highlight.join(' · ');
  }

  const general =
    form.generalCondition === 'Other'
      ? form.generalConditionOther.trim() || 'Other'
      : form.generalCondition;
  const postop =
    form.postopCourse === 'Complicated'
      ? `Complicated${form.postopCourseDetails.trim() ? ` — ${form.postopCourseDetails.trim()}` : ''}`
      : form.postopCourse;

  const plan = pickLabel(form.followUpKey, form.followUpOther);
  const notes = form.followUpNotes.trim();
  const followUp = plan && notes ? `${plan}. ${notes}` : plan || notes;

  const meds = (form.medicines ?? []).filter(m => m.name.trim());
  const medications = meds
    .map((m, i) => {
      const bits = [
        [m.name, m.strength].filter(Boolean).join(' '),
        m.dose,
        m.frequency,
        m.duration,
        m.route,
        m.timing
      ]
        .map(x => x.trim())
        .filter(Boolean);
      return `${i + 1}. ${bits.join(' · ')}`;
    })
    .join('\n');

  return {
    ...form,
    finalDiagnosis: diagnosis || form.finalDiagnosis,
    procedureDetails: procedureDetails || form.procedureDetails,
    findings: findingLines.join('\n') || form.findings,
    conditionAtDischarge: [general, `Postop: ${postop}`, form.dischargeType].filter(Boolean).join(' · ') || form.conditionAtDischarge,
    followUp: followUp || form.followUp,
    medications: medications || form.medications
  };
}

export function applyDiagnosisPack(form: DischargeForm, master: DiagnosisMaster): DischargeForm {
  const pack = master.pack ?? {};
  const size = pack['medicinePackSize'] as number | null | undefined;
  const packMeds = commonMedicinePack().map((m, i) => ({
    ...m,
    id: `seed-med-${master.code}-${i}`
  }));
  const medicines = size == null ? packMeds : packMeds.slice(0, size);

  return {
    ...form,
    diagnosisKey: master.name,
    diagnosisOther: '',
    finalDiagnosis: master.name,
    chiefComplaints: String(pack['chiefComplaints'] ?? form.chiefComplaints),
    briefHistory: String(pack['briefHistory'] ?? form.briefHistory),
    advice: String(pack['advice'] ?? form.advice),
    procedureKey: (pack['procedureKey'] as DischargeProcedureKey) || form.procedureKey,
    procedureExtras: { ...((pack['procedureExtras'] as Record<string, string>) ?? {}) },
    catheterKey: String(pack['catheterKey'] ?? form.catheterKey),
    catheterOther: String(pack['catheterOther'] ?? ''),
    specimen: String(pack['specimen'] ?? form.specimen),
    dischargeType: String(pack['dischargeType'] ?? form.dischargeType),
    followUpKey: String(pack['followUpKey'] ?? form.followUpKey),
    followUpOther: String(pack['followUpOther'] ?? ''),
    followUpNotes: String(pack['followUpNotes'] ?? form.followUpNotes),
    generalCondition: String(pack['generalCondition'] ?? form.generalCondition),
    postopCourse: String(pack['postopCourse'] ?? form.postopCourse),
    pastHistory: {
      comorbidities: String((pack['pastHistory'] as any)?.comorbidities ?? ''),
      pastSurgery: String((pack['pastHistory'] as any)?.pastSurgery ?? ''),
      allergy: String((pack['pastHistory'] as any)?.allergy ?? ''),
      addiction: String((pack['pastHistory'] as any)?.addiction ?? '')
    },
    treatmentDuringAdmission: String(pack['treatmentDuringAdmission'] ?? form.treatmentDuringAdmission),
    anaesthesia: String(pack['anaesthesia'] ?? form.anaesthesia),
    indication: String(pack['indication'] ?? form.indication),
    medicines
  };
}

export function normalizeForm(raw: any, admissionId: string): DischargeForm {
  const invKeys = INVESTIGATION_ROWS.map(r => r.key);
  const emptyInv = Object.fromEntries(invKeys.map(k => [k, '']));
  return {
    admissionId,
    dateOfSurgery: raw?.dateOfSurgery ?? '',
    dateOfDischarge: raw?.dateOfDischarge ?? '',
    finalDiagnosis: raw?.finalDiagnosis ?? '',
    chiefComplaints: raw?.chiefComplaints ?? '',
    briefHistory: raw?.briefHistory ?? '',
    investigations: { ...emptyInv, ...(raw?.investigations ?? {}) },
    investigationDates: { ...emptyInv, ...(raw?.investigationDates ?? {}) },
    procedureDetails: raw?.procedureDetails ?? '',
    findings: raw?.findings ?? '',
    conditionAtDischarge: raw?.conditionAtDischarge ?? '',
    medications: raw?.medications ?? '',
    advice: raw?.advice ?? '',
    followUp: raw?.followUp ?? '',
    fatherName: raw?.fatherName ?? '',
    pastHistory: {
      comorbidities: raw?.pastHistory?.comorbidities ?? '',
      pastSurgery: raw?.pastHistory?.pastSurgery ?? '',
      allergy: raw?.pastHistory?.allergy ?? '',
      addiction: raw?.pastHistory?.addiction ?? ''
    },
    examination: {
      general: raw?.examination?.general ?? '',
      pulse: raw?.examination?.pulse ?? '',
      bp: raw?.examination?.bp ?? '',
      temp: raw?.examination?.temp ?? '',
      spo2: raw?.examination?.spo2 ?? '',
      systemic: raw?.examination?.systemic ?? '',
      perAbdomen: raw?.examination?.perAbdomen ?? '',
      genitalia: raw?.examination?.genitalia ?? ''
    },
    treatmentDuringAdmission: raw?.treatmentDuringAdmission ?? '',
    anaesthesia: raw?.anaesthesia ?? '',
    assistantSurgeon: raw?.assistantSurgeon ?? '',
    indication: raw?.indication ?? '',
    intraopComplications: raw?.intraopComplications ?? 'Nil',
    conditionExtras: {
      pulse: raw?.conditionExtras?.pulse ?? '',
      bp: raw?.conditionExtras?.bp ?? '',
      temp: raw?.conditionExtras?.temp ?? '',
      spo2: raw?.conditionExtras?.spo2 ?? '',
      urineOutput: raw?.conditionExtras?.urineOutput ?? '',
      oralIntake: raw?.conditionExtras?.oralIntake ?? '',
      ambulation: raw?.conditionExtras?.ambulation ?? ''
    },
    followUpDate: raw?.followUpDate ?? '',
    followUpDepartment: raw?.followUpDepartment ?? 'Urology OPD',
    followUpInvestigations: raw?.followUpInvestigations ?? '',
    procedureKey: (raw?.procedureKey as DischargeProcedureKey) ?? '',
    procedureExtras: { ...(raw?.procedureExtras ?? {}) },
    diagnosisKey: raw?.diagnosisKey ?? '',
    diagnosisOther: raw?.diagnosisOther ?? '',
    generalCondition: raw?.generalCondition ?? 'Stable',
    generalConditionOther: raw?.generalConditionOther ?? '',
    postopCourse: raw?.postopCourse ?? 'Uneventful',
    postopCourseDetails: raw?.postopCourseDetails ?? '',
    catheterKey: raw?.catheterKey ?? '',
    catheterOther: raw?.catheterOther ?? '',
    specimen: raw?.specimen ?? 'Not applicable',
    dischargeType: raw?.dischargeType ?? 'Routine discharge',
    followUpKey: raw?.followUpKey ?? 'Routine OPD follow-up',
    followUpOther: raw?.followUpOther ?? '',
    followUpNotes: raw?.followUpNotes ?? '',
    medicines: Array.isArray(raw?.medicines)
      ? raw.medicines.map((m: any, i: number) => ({
          id: m.id || `med_${i}`,
          name: m.name ?? '',
          strength: m.strength ?? '',
          dose: m.dose ?? '',
          route: m.route ?? 'Oral',
          frequency: m.frequency ?? '',
          duration: m.duration ?? '',
          timing: m.timing ?? ''
        }))
      : []
  };
}
