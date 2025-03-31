import React from 'react';
import { Navbar, Nav, Container } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faFileUpload, faListAlt } from '@fortawesome/free-solid-svg-icons';

const Navigation = () => {
  return (
    <Navbar bg="dark" variant="dark" expand="lg">
      <Container>
        <Navbar.Brand as={Link} to="/">PDF OCR Processor</Navbar.Brand>
        <Navbar.Toggle aria-controls="basic-navbar-nav" />
        <Navbar.Collapse id="basic-navbar-nav">
          <Nav className="me-auto">
            <Nav.Link as={Link} to="/">
              <FontAwesomeIcon icon={faFileUpload} className="me-2" />
              Upload
            </Nav.Link>
            <Nav.Link as={Link} to="/documents">
              <FontAwesomeIcon icon={faListAlt} className="me-2" />
              Documents
            </Nav.Link>
          </Nav>
        </Navbar.Collapse>
      </Container>
    </Navbar>
  );
};

export default Navigation;