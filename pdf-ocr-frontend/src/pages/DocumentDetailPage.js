import React, { useState, useEffect } from 'react';
import { 
  Card, 
  Row, 
  Col, 
  Table, 
  Badge, 
  Button, 
  Spinner, 
  Alert,
  Tab,
  Nav,
  ListGroup
} from 'react-bootstrap';
import { useParams, Link } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faFileAlt, 
  faArrowLeft, 
  faDownload,
  faCheckCircle,
  faExclamationTriangle,
  faInfoCircle,
  faTable,
  faCode
} from '@fortawesome/free-solid-svg-icons';
import axios from 'axios';
import { API_URL } from '../config';

const DocumentDetailPage = () => {
  const { id } = useParams();
  const [document, setDocument] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [jsonData, setJsonData] = useState('');
  
  useEffect(() => {
    fetchDocument();
  }, [id]);
  
  const fetchDocument = async () => {
    setLoading(true);
    try {
      const response = await axios.get(
        `${API_URL}/api/documents/${id}`
      );
      setDocument(response.data);
      
      // Fetch JSON data
      const jsonResponse = await axios.get(
        `${API_URL}/api/documents/${id}/json`,
        { responseType: 'text' }
      );
      setJsonData(jsonResponse.data);
      
      setError('');
    } catch (err) {
      setError('Failed to fetch document details');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };
  
  const handleDownloadJson = () => {
    const element = document.createElement('a');
    const file = new Blob([jsonData], { type: 'application/json' });
    element.href = URL.createObjectURL(file);
    element.download = `${document.fileName.replace('.pdf', '')}.json`;
    document.body.appendChild(element);
    element.click();
    document.body.removeChild(element);
  };
  
  const handleDownloadCsv = async () => {
    try {
      const response = await axios.get(
        `${API_URL}/api/documents/${id}/csv`,
        { responseType: 'blob' }
      );
      
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `${document.fileName.replace('.pdf', '')}.csv`);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    } catch (err) {
      setError('Failed to download CSV');
      console.error(err);
    }
  };
  
  const formatDate = (dateString) => {
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  };
  
  if (loading) {
    return (
      <div className="text-center py-5">
        <Spinner animation="border" role="status" variant="primary">
          <span className="visually-hidden">Loading...</span>
        </Spinner>
        <p className="mt-2">Loading document details...</p>
      </div>
    );
  }
  
  if (error) {
    return (
      <Alert variant="danger">
        <Alert.Heading>Error</Alert.Heading>
        <p>{error}</p>
        <div className="d-flex justify-content-end">
          <Link to="/documents" className="btn btn-outline-secondary">
            <FontAwesomeIcon icon={faArrowLeft} className="me-2" />
            Back to Documents
          </Link>
        </div>
      </Alert>
    );
  }
  
  if (!document) {
    return (
      <Alert variant="warning">
        <Alert.Heading>Document Not Found</Alert.Heading>
        <p>The requested document could not be found.</p>
        <div className="d-flex justify-content-end">
          <Link to="/documents" className="btn btn-outline-secondary">
            <FontAwesomeIcon icon={faArrowLeft} className="me-2" />
            Back to Documents
          </Link>
        </div>
      </Alert>
    );
  }
  
  return (
    <div className="document-detail-page">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>
          <FontAwesomeIcon icon={faFileAlt} className="me-2 text-primary" />
          {document.fileName}
        </h2>
        
        <Link to="/documents" className="btn btn-outline-secondary">
          <FontAwesomeIcon icon={faArrowLeft} className="me-2" />
          Back to Documents
        </Link>
      </div>
      
      <Row className="mb-4">
        <Col md={4}>
          <Card className="shadow-sm h-100">
            <Card.Header as="h5">Document Information</Card.Header>
            <Card.Body>
              <ListGroup variant="flush">
                <ListGroup.Item>
                  <strong>Document Type:</strong>{' '}
                  <Badge bg="info">{document.documentType || 'Unknown'}</Badge>
                </ListGroup.Item>
                <ListGroup.Item>
                  <strong>Status:</strong>{' '}
                  {document.isSuccessful ? (
                    <Badge bg="success">
                      <FontAwesomeIcon icon={faCheckCircle} className="me-1" />
                      Processed Successfully
                    </Badge>
                  ) : (
                    <Badge bg="danger">
                      <FontAwesomeIcon icon={faExclamationTriangle} className="me-1" />
                      Processing Failed
                    </Badge>
                  )}
                </ListGroup.Item>
                <ListGroup.Item>
                  <strong>Processed Date:</strong>{' '}
                  {formatDate(document.processedDate)}
                </ListGroup.Item>
              </ListGroup>
            </Card.Body>
            <Card.Footer>
              <div className="d-grid gap-2">
                <Button 
                  variant="outline-primary" 
                  onClick={handleDownloadJson}
                  disabled={!document.isSuccessful}
                >
                  <FontAwesomeIcon icon={faDownload} className="me-2" />
                  Download JSON
                </Button>
                <Button 
                  variant="outline-success" 
                  onClick={handleDownloadCsv}
                  disabled={!document.isSuccessful}
                >
                  <FontAwesomeIcon icon={faDownload} className="me-2" />
                  Download CSV
                </Button>
              </div>
            </Card.Footer>
          </Card>
        </Col>
        
        <Col md={8}>
          <Card className="shadow-sm">
            <Card.Header as="h5">Extracted Fields</Card.Header>
            <Card.Body>
              {!document.isSuccessful ? (
                <Alert variant="danger">
                  <FontAwesomeIcon icon={faExclamationTriangle} className="me-2" />
                  Processing failed: {document.errorMessage || 'Unknown error'}
                </Alert>
              ) : document.fields.length === 0 ? (
                <Alert variant="info">
                  <FontAwesomeIcon icon={faInfoCircle} className="me-2" />
                  No fields were extracted from this document.
                </Alert>
              ) : (
                <div className="table-responsive">
                  <Table striped bordered hover>
                    <thead>
                      <tr>
                        <th>Field Name</th>
                        <th>Value</th>
                        <th>Confidence</th>
                      </tr>
                    </thead>
                    <tbody>
                      {document.fields.map((field, index) => (
                        <tr key={index}>
                          <td><strong>{field.name}</strong></td>
                          <td>{field.value}</td>
                          <td>
                            {field.confidence >= 0.9 ? (
                              <Badge bg="success">{(field.confidence * 100).toFixed(1)}%</Badge>
                            ) : field.confidence >= 0.7 ? (
                              <Badge bg="warning" text="dark">{(field.confidence * 100).toFixed(1)}%</Badge>
                            ) : (
                              <Badge bg="danger">{(field.confidence * 100).toFixed(1)}%</Badge>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </Table>
                </div>
              )}
            </Card.Body>
          </Card>
        </Col>
      </Row>
      
      <Card className="shadow-sm">
        <Tab.Container id="data-tabs" defaultActiveKey="table">
          <Card.Header>
            <Nav variant="tabs">
              <Nav.Item>
                <Nav.Link eventKey="table">
                  <FontAwesomeIcon icon={faTable} className="me-2" />
                  Structured Data
                </Nav.Link>
              </Nav.Item>
              <Nav.Item>
                <Nav.Link eventKey="json">
                  <FontAwesomeIcon icon={faCode} className="me-2" />
                  JSON Data
                </Nav.Link>
              </Nav.Item>
            </Nav>
          </Card.Header>
          <Card.Body>
            <Tab.Content>
              <Tab.Pane eventKey="table">
                {!document.isSuccessful ? (
                  <Alert variant="danger">
                    <FontAwesomeIcon icon={faExclamationTriangle} className="me-2" />
                    Processing failed: {document.errorMessage || 'Unknown error'}
                  </Alert>
                ) : document.fields.length === 0 ? (
                  <Alert variant="info">
                    <FontAwesomeIcon icon={faInfoCircle} className="me-2" />
                    No structured data available for this document.
                  </Alert>
                ) : (
                  <div className="table-responsive">
                    <Table striped bordered hover>
                      <thead>
                        <tr>
                          <th>Field Name</th>
                          <th>Value</th>
                        </tr>
                      </thead>
                      <tbody>
                        {document.fields.map((field, index) => (
                          <tr key={index}>
                            <td width="30%"><strong>{field.name}</strong></td>
                            <td>{field.value}</td>
                          </tr>
                        ))}
                      </tbody>
                    </Table>
                  </div>
                )}
              </Tab.Pane>
              <Tab.Pane eventKey="json">
                <pre className="bg-light p-3 border rounded" style={{ maxHeight: '400px', overflow: 'auto' }}>
                  {jsonData ? JSON.stringify(JSON.parse(jsonData), null, 2) : 'No JSON data available'}
                </pre>
              </Tab.Pane>
            </Tab.Content>
          </Card.Body>
        </Tab.Container>
      </Card>
    </div>
  );
};

export default DocumentDetailPage;