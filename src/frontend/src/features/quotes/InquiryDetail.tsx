import { useState } from "react";
import type {
  CustomerInquiryDto,
  InquiryStatus,
} from "../../shared/api/contracts";

export function InquiryDetail({
  row,
  onAddActivity,
  onChangeStatus,
  onUpload,
  onDownload,
  onCreateQuote,
}: {
  row: CustomerInquiryDto;
  onAddActivity: (body: string) => void;
  onChangeStatus: (status: InquiryStatus, memo?: string) => void;
  onUpload: (file: File) => void;
  onDownload?: (attachmentId: string, fileName: string) => void;
  onCreateQuote: () => void;
}) {
  const [memo, setMemo] = useState("");
  return (
    <article className="quote-detail">
      <div className="detail-heading">
        <div>
          <p>{row.inquiryNumber}</p>
          <h2>{row.title}</h2>
        </div>
        <span className="status-badge">{row.status}</span>
      </div>
      <dl className="company-summary">
        <div>
          <dt>거래처</dt>
          <dd>{row.customerName}</dd>
        </div>
        <div>
          <dt>연락 담당자</dt>
          <dd>{row.contactName}</dd>
        </div>
        <div>
          <dt>접수일</dt>
          <dd>{row.receivedAt}</dd>
        </div>
        <div>
          <dt>답변기한</dt>
          <dd>{row.responseDueAt}</dd>
        </div>
      </dl>
      <p>{row.summary}</p>
      <div className="detail-actions">
        <button type="button" onClick={onCreateQuote}>
          연결 견적 만들기
        </button>
        <button
          type="button"
          className="secondary-action"
          onClick={() => onAddActivity("전화 상담 완료")}
        >
          활동 추가
        </button>
        <label>
          상태 변경 메모
          <textarea
            aria-label="상태 변경 메모"
            value={memo}
            onChange={(event) => setMemo(event.target.value)}
            placeholder="종료 시 결과 메모 필수"
          />
        </label>
        <label>
          상태 변경
          <select
            aria-label="문의 상태"
            value={row.status}
            onChange={(event) =>
              onChangeStatus(
                event.target.value as InquiryStatus,
                memo.trim() || undefined,
              )
            }
          >
            <option value="received">접수</option>
            <option value="assigned">담당자 배정</option>
            <option value="responding">대응 중</option>
            <option value="quoting">견적 작성</option>
            <option value="answered">답변 완료</option>
            <option value="closed">종료</option>
          </select>
        </label>
        <label>
          첨부 등록
          <input
            aria-label="문의 첨부파일"
            type="file"
            onChange={(event) => {
              const file = event.target.files?.[0];
              if (file) onUpload(file);
            }}
          />
        </label>
      </div>
      <h3>활동 및 첨부</h3>
      {row.activities.length ? (
        row.activities.map((item) => (
          <p key={item.id}>
            {item.occurredAt} · {item.body}
          </p>
        ))
      ) : (
        <p>등록된 활동이 없습니다.</p>
      )}
      {row.attachments.map((file) => (
        <p key={file.id}>
          📎{" "}
          <button
            type="button"
            className="link-action"
            aria-label={`${file.fileName} 다운로드`}
            onClick={() => onDownload?.(file.id, file.fileName)}
          >
            {file.fileName}
          </button>
        </p>
      ))}
      <p>
        첨부파일 {row.attachments.length}개 · 연결 견적{" "}
        {row.linkedQuoteIds.length}건
      </p>
    </article>
  );
}
